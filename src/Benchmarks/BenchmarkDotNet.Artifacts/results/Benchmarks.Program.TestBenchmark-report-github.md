``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.472 (1803/April2018Update/Redstone4)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.101
  [Host] : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT
  Core   : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT

Job=Core  Jit=RyuJit  Platform=X64  
Runtime=Core  IterationTime=200.0000 ms  LaunchCount=1  

```
|                 Method |      Mean |     Error |    StdDev |
|----------------------- |----------:|----------:|----------:|
|           TestAddition | 1.0042 ns | 0.0092 ns | 0.0077 ns |
|       TestAdditionLong | 0.1675 ns | 0.0111 ns | 0.0099 ns |
| TestInterlockedIncLong | 4.7559 ns | 0.0233 ns | 0.0194 ns |
|              TestEvent | 2.2507 ns | 0.0099 ns | 0.0083 ns |
