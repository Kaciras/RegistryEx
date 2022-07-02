using BenchmarkDotNet.Running;
using RegistryHelper.Benchmark;

BenchmarkRunner.Run<RegFilePerf>();
Console.ReadKey();
