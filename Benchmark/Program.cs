using BenchmarkDotNet.Running;
using RegistryEx;
using RegistryEx.Benchmark;
using RegistryEx.Test;

BenchmarkRunner.Run<ImportRegFile>();

RegistryHelper.RemoveTokenPrivileges();
TestFixture.Import("Kinds");
