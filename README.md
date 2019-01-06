# prometheus-net.DotNetMetrics
A plugin for the [prometheus-net](https://github.com/prometheus-net/prometheus-net) package, exposing .NET core runtime metrics including:
- Garbage collection frequencies, pauses and timings
- Heap size by generation
- Bytes allocated
- JIT compilations
- Thread pool size, scheduling delays and reasons for growing/ shrinking
- Lock contention

These metrics are essential for understanding the peformance of any non-trivial application. Even if your application is well instrumented, you're only getting half the story- what the runtime is doing completes the picture.

# Installation
_Requires .NET core v2.2+_.

Add the packge using:
```
dotnet add package prometheus-net.DotNetRuntime --version 0.0.4-alpha
```

And then register the collector:
```
 DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(DotNetRuntimeStatsBuilder.Default());
```



