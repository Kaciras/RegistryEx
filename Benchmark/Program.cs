using BenchmarkDotNet.Running;
using RegistryEx.Benchmark;

BenchmarkRunner.Run<RegFileWriterPerf>();
Console.ReadKey();
