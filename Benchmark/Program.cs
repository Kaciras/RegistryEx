using BenchmarkDotNet.Running;
using Microsoft.Win32;
using RegistryEx;
using RegistryEx.Benchmark;
using RegistryEx.Test;

//BenchmarkRunner.Run<ImportRegFile>();

//RegistryHelper.RemoveTokenPrivileges();
//TestFixture.Import("Kinds");
try
{
	RegistryHelper.Elevate(Registry.CurrentUser, "_RH_Test_");
}
finally
{
	//RegistryHelper.AddTokenPrivileges();
}

Console.ReadKey();
