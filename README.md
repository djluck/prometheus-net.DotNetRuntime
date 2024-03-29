# prometheus-net.DotNetMetrics
A plugin for the [prometheus-net](https://github.com/prometheus-net/prometheus-net) package, [exposing .NET core runtime metrics](docs/metrics-exposed.md) including:
- Garbage collection collection frequencies and timings by generation/ type, pause timings and GC CPU consumption ratio
- Heap size by generation
- Bytes allocated by small/ large object heap
- JIT compilations and JIT CPU consumption ratio
- Thread pool size, scheduling delays and reasons for growing/ shrinking
- Lock contention
- Exceptions thrown, broken down by type

These metrics are essential for understanding the performance of any non-trivial application. Even if your application is well instrumented, you're only getting half the story- what the runtime is doing completes the picture.

## Using this package
### Requirements
- .NET 5.0+ recommended, .NET core 3.1+ is supported 
- The [prometheus-net](https://github.com/prometheus-net/prometheus-net) package

### Install it
The package can be installed from [nuget](https://www.nuget.org/packages/prometheus-net.DotNetRuntime):
```powershell
dotnet add package prometheus-net.DotNetRuntime
```

### Start collecting metrics
You can start metric collection with:
```csharp
IDisposable collector = DotNetRuntimeStatsBuilder.Default().StartCollecting()
```

You can customize the types of .NET metrics collected via the `Customize` method:
```csharp
IDisposable collector = DotNetRuntimeStatsBuilder
	.Customize()
	.WithContentionStats()
	.WithJitStats()
	.WithThreadPoolStats()
	.WithGcStats()
	.WithExceptionStats()
	.StartCollecting();
```

Once the collector is registered, you should see metrics prefixed with `dotnet_` visible in your metric output (make sure you are [exporting your metrics](https://github.com/prometheus-net/prometheus-net#http-handler)).

### Choosing a `CaptureLevel`
By default the library will default generate metrics based on [event counters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counters). This allows for basic instrumentation of applications with very little performance overhead. 

You can enable higher-fidelity metrics by providing a custom `CaptureLevel`, e.g:
```
DotNetRuntimeStatsBuilder
	.Customize()
	.WithGcStats(CaptureLevel.Informational)
	.WithExceptionStats(CaptureLevel.Errors)
	...
```

Most builder methods allow the passing of a custom `CaptureLevel`- see the [documentation on exposed metrics](docs/metrics-exposed.md) for more information.

### Performance impact of `CaptureLevel.Errors`+
The harder you work the .NET core runtime, the more events it generates. Event generation and processing costs can stack up, especially around these types of events:
- **JIT stats**: each method compiled by the JIT compiler emits two events. Most JIT compilation is performed at startup and depending on the size of your application, this could impact your startup performance.
- **GC stats with `CaptureLevel.Verbose`**: every 100KB of allocations, an event is emitted. If you are consistently allocating memory at a rate > 1GB/sec, you might like to disable GC stats.
- **Exception stats with `CaptureLevel.Errors`**: for every exception throw, an event is generated.

#### Recycling collectors
There have been long-running [performance issues since .NET core 3.1](https://github.com/dotnet/runtime/issues/43985#issuecomment-800629516) that could see CPU consumption grow over time when long-running trace sessions are used. 
While many of the performance issues have been addressed now in .NET 6.0, a workaround was identified: stopping and starting (AKA recycling) collectors periodically helped reduce CPU consumption:
```
IDisposable collector = DotNetRuntimeStatsBuilder.Default()
	// Recycles all collectors once every day
	.RecycleCollectorsEvery(TimeSpan.FromDays(1))
	.StartCollecting()
```

While this [has been observed to reduce CPU consumption](https://github.com/djluck/prometheus-net.DotNetRuntime/issues/6#issuecomment-784540220) this technique has been identified as a [possible culprit that can lead
to application instability](https://github.com/djluck/prometheus-net.DotNetRuntime/issues/72). 

Behaviour on different runtime versions is:
- .NET core 3.1: recycling verified to cause massive instability, cannot enable recycling.
- .NET 5.0: recycling verified to be beneficial, recycling every day enabled by default.
- .NET 6.0+: recycling verified to be less necesarry due to long-standing issues being addressed although [some users report recycling to be beneficial](https://github.com/djluck/prometheus-net.DotNetRuntime/pull/73#issuecomment-1308558226), 
  disabled by default but recycling can be enabled.
  
> TLDR: If you observe increasing CPU over time, try enabling recycling. If you see unexpected crashes after using this application, try disabling recycling.


## Examples
An example `docker-compose` stack is available in the [`examples/`](examples/) folder. Start it with:

```
docker-compose up -d
```

You can then visit [`http://localhost:3000`](http://localhost:3000) to view metrics being generated by a sample application.

### Grafana dashboard
The metrics exposed can drive a rich dashboard, giving you a graphical insight into the performance of your application ( [exported dashboard available here](examples/grafana/provisioning/dashboards/NET_runtime_metrics_dashboard.json)):

![Grafana dashboard sample](docs/grafana-example.PNG)

## Further reading 
- The mechanism for listening to runtime events is outlined in the [.NET core 2.2 release notes](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-2-2#core).
- A partial list of core CLR events is available in the [ETW events documentation](https://docs.microsoft.com/en-us/dotnet/framework/performance/clr-etw-events).