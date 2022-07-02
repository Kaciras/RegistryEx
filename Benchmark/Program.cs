using BenchmarkDotNet.Running;
using RegistryEx.Benchmark;

BenchmarkRunner.Run<RegFilePerf>();
Console.ReadKey();
