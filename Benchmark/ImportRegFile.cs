using BenchmarkDotNet.Attributes;
using Microsoft.Win32;
using RegistryEx.Test;

namespace RegistryEx.Benchmark;

public class ImportRegFile
{
	[GlobalCleanup]
	public void Cleanup()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_TH_Test_", false);
	}

	[Benchmark]
	public object Document()
	{
		var doc = RegDocument.ParseFile(@"Resources/Redundant.reg");
		doc.Execute();
		return doc;
	}

	[Benchmark]
	public void Regedit()
	{
		SharedTools.Import(@"Resources/Redundant.reg");
	}
}
