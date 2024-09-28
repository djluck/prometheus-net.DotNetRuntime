﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fasterflect;
using Grynwald.MarkdownGenerator;
using Prometheus;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace DocsGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Do a HTTP request to trigger DNS request + open socket
            using var client = new HttpClient();
            client.GetAsync("https://httpstat.us/200").Wait();

            // TODO output different path depending on runtime version
            var sources = new []
            {
                SourceAndConfig.CreateFrom(b => b.WithThreadPoolStats(CaptureLevel.Counters, new ThreadPoolMetricsProducer.Options())),
                SourceAndConfig.CreateFrom(b => b.WithThreadPoolStats(CaptureLevel.Informational, new ThreadPoolMetricsProducer.Options())),
                SourceAndConfig.CreateFrom(b => b.WithGcStats(CaptureLevel.Counters, null)),
                SourceAndConfig.CreateFrom(b => b.WithGcStats(CaptureLevel.Informational, null)),
                SourceAndConfig.CreateFrom(b => b.WithGcStats(CaptureLevel.Verbose, null)),
                SourceAndConfig.CreateFrom(b => b.WithContentionStats(CaptureLevel.Counters, SampleEvery.OneEvent)),
                SourceAndConfig.CreateFrom(b => b.WithContentionStats(CaptureLevel.Informational, SampleEvery.OneEvent)),
                SourceAndConfig.CreateFrom(b => b.WithExceptionStats(CaptureLevel.Counters)),
                SourceAndConfig.CreateFrom(b => b.WithExceptionStats(CaptureLevel.Errors)),
                SourceAndConfig.CreateFrom(b => b.WithJitStats(CaptureLevel.Counters, SampleEvery.OneEvent)),
                SourceAndConfig.CreateFrom(b => b.WithJitStats(CaptureLevel.Verbose, SampleEvery.OneEvent)),
                SourceAndConfig.CreateFrom(b => b.WithExceptionStats(CaptureLevel.Errors)),
                SourceAndConfig.CreateFrom(b => b.WithSocketStats()),
                SourceAndConfig.CreateFrom(b => b.WithNameResolution(null))
            };

            var assemblyDocs = typeof(DotNetRuntimeStatsBuilder).Assembly.LoadXmlDocumentation();
            
            var allMetrics = GetAllMetrics(sources);

            MdSpan[] GetCells(Collector m)
            {
                return new MdSpan[]
                {
                    new MdRawMarkdownSpan($"`{m.Name}`"),
                    new MdRawMarkdownSpan($"`{m.GetType().Name}`"),
                    m.Help,
                    new MdRawMarkdownSpan(string.Join(", ", m.LabelNames.Select(x => $"`{x}`"))),
                };
            }

         
            
            var document = new MdDocument();
            var root = document.Root;
            root.Add(new MdHeading(new MdRawMarkdownSpan($"`.net {EventParserTypes.CurrentRuntimeVerison.Value}` metrics"), 1));
            root.Add(new MdParagraph(new MdRawMarkdownSpan($"Each subheading details the metrics produced by calling builder methods with the specified `{nameof(CaptureLevel)}`.")));

            root.Add(new MdHeading("Default metrics", 2));
            root.Add(new MdParagraph("Metrics that are included by default, regardless of what stats collectors are enabled."));

            root.Add(new MdTable(headerRow: new MdTableRow("Name", "Type", "Description", "Labels"), 
                allMetrics.CommonMetrics.OrderBy(x => x.Name).Select(x => new MdTableRow(GetCells(x))).ToArray()));
                
            foreach (var methodAndSources in allMetrics.MethodsToSources.OrderBy(x => x.method.Name))
            {
                root.Add(new MdHeading(new MdCodeSpan($".{methodAndSources.method.Name}()"), 2));

                var nonEmptySources = methodAndSources.sources
                    .Where(s => allMetrics.SourceToMetrics[s].Count > 0)
                    .ToList();

                if (nonEmptySources.Count == 0)
                {
                    root.Add(new MdParagraph("This method does not export any metrics on this version of the framework."));
                    continue;
                }

                root.Add(new MdParagraph(assemblyDocs.GetDocumentation(methodAndSources.method).Summary));

                for (var i = 0; i < nonEmptySources.Count; i++)
                {
                    var s = nonEmptySources[i];
                    root.Add(new MdHeading(new MdCodeSpan($"{nameof(CaptureLevel)}." + s.Level), 3));
                    
                    var previousLevels = nonEmptySources.Take(i).ToArray();
                    if (previousLevels.Length > 0)
                    {
                        root.Add(new MdParagraph(
                            new MdRawMarkdownSpan($"Includes metrics generated by {string.Join(", ", previousLevels.Select(c => $"`{nameof(CaptureLevel)}.{c.Level}`"))} plus:")
                        ));
                    }

                    root.Add(new MdTable(headerRow: new MdTableRow("Name", "Type", "Description", "Labels"), 
                        allMetrics.SourceToMetrics[s].OrderBy(x => x.Collector.Name).Select(x => new MdTableRow(GetCells(x.Collector))).ToArray()));
                }
            }
            
            
            document.Save($"../../../../../docs/metrics-exposed-{EventParserTypes.CurrentRuntimeVerison.Value}.md");
        }

        private static GroupedMetrics GetAllMetrics(SourceAndConfig[] sources)
        {
            var methodsOrderedByLevel = sources
                .GroupBy(
                    x => x.Source.Method, 
                    v => v,
                    (m, levels) => (method: m, orderedConfigs: levels.OrderBy(l => l.Source.Level).ToArray())
                )
                .ToArray();
            
            // Find all metrics exposed
            var sourceToMetrics = methodsOrderedByLevel
                .SelectMany(x =>
                    x.orderedConfigs
                        // Build up metrics, going from lowest capture level (counters) to highest capture level
                        // This ensures metrics present across multiple capture levels are only recorded once
                        .Aggregate(
                            ImmutableHashSet<ExposedMetric>.Empty.WithComparer(new MetricEquality()), 
                            (acc, next) => acc.Union(GetExposedMetric(next))
                        )
                )
                .GroupBy(x => x.Source)
                .ToDictionary(k => k.Key, v => v.ToList());

            // Find common metrics
            var commonMetricsGrouped = sourceToMetrics.SelectMany(v => v.Value)
                .GroupBy(x => x, new MetricEquality())
                .Where(x => x.Count() == methodsOrderedByLevel.Length);

            var commonExposedMetrics = commonMetricsGrouped
                .Select(x => x.Key)
                .ToHashSet(new MetricEquality());

            // Remove common metrics from all sources
            foreach (var s in sourceToMetrics)
                s.Value.RemoveAll(x => commonExposedMetrics.Contains(x));

            return new GroupedMetrics(
                commonExposedMetrics.Select(x => x.Collector).ToImmutableList(),
                sourceToMetrics.ToImmutableDictionary(
                    k => k.Key, 
                    v => v.Value.Select(i => i).ToImmutableList()
                )
            );
        }

        private static IEnumerable<ExposedMetric> GetExposedMetric(SourceAndConfig source)
        {
            Console.WriteLine($"Getting metrics for {source.Source}..");

            // Start collector
            var registry = new CollectorRegistry();
            using var statsCollector = source.ApplyConfig(DotNetRuntimeStatsBuilder.Customize()).StartCollecting(registry) as DotNetRuntimeStatsCollector;

            // Pull registered collectors
            var collectors = registry.TryGetFieldValue("_collectors", Flags.InstancePrivate) as ConcurrentDictionary<string, Collector>;

            // Wait for all listeners to be ready
            SpinWait.SpinUntil(
                () => statsCollector.EventListeners.All(x => x.StartedReceivingEvents),
                TimeSpan.FromSeconds(5));

            return collectors.Values.Select(c => new ExposedMetric(c, source.Source));
        }

        public record Source(MethodInfo Method, CaptureLevel Level)
        {
            public CaptureLevel Level { get; } = Level;
            public MethodInfo Method { get; } = Method;
        }
        
        public record SourceAndConfig(Source Source, Func<DotNetRuntimeStatsBuilder.Builder, DotNetRuntimeStatsBuilder.Builder> ApplyConfig)
        {
            public Source Source { get; } = Source;
            public Func<DotNetRuntimeStatsBuilder.Builder, DotNetRuntimeStatsBuilder.Builder> ApplyConfig { get; } = ApplyConfig;

            public static SourceAndConfig CreateFrom(Expression<Func<DotNetRuntimeStatsBuilder.Builder, DotNetRuntimeStatsBuilder.Builder>> fromMethod)
            {
                var mCall = (fromMethod.Body as MethodCallExpression);
                var method = mCall.Method;
                var captureLevelArg = mCall?.Arguments.OfType<ConstantExpression>().SingleOrDefault(x => x.Type == typeof(CaptureLevel))?.Value;
                
                var captureLevel = captureLevelArg != null ? ( CaptureLevel)captureLevelArg : CaptureLevel.Counters;

                return new SourceAndConfig(new Source(method, captureLevel), fromMethod.Compile());
            }
        }

        public record ExposedMetric(Collector Collector, Source Source)
        {
            public Collector Collector { get; } = Collector;
            public Source Source { get; } = Source;
            
            public string Type => Collector.GetType().Name;
            public string Labels => string.Join(", ", Collector.LabelNames);
        }

        public class MetricEquality : IEqualityComparer<ExposedMetric>
        {
            public bool Equals(ExposedMetric x, ExposedMetric y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Collector.Name == y.Collector.Name && x.Collector.Help == y.Collector.Help && x.Labels == y.Labels && x.Type == y.Type;
            }

            public int GetHashCode(ExposedMetric obj)
            {
                return HashCode.Combine(obj.Collector.Name, obj.Collector.Help, obj.Labels, obj.Type);
            }
        }
        
        public record GroupedMetrics(ImmutableList<Collector> CommonMetrics, ImmutableDictionary<Source, ImmutableList<ExposedMetric>> SourceToMetrics)
        {
            public ImmutableDictionary<Source, ImmutableList<ExposedMetric>> SourceToMetrics { get; } = SourceToMetrics;
            public ImmutableList<Collector> CommonMetrics { get; } = CommonMetrics;
            public IEnumerable<(MethodInfo method, Source[] sources)> MethodsToSources => SourceToMetrics.Keys.GroupBy(x => x.Method, (k, v) => (method: k, sources: v.OrderBy(x => x.Level).ToArray()));
        }
    }
}
