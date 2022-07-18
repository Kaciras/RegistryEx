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
	public object @RegDocument()
	{
		var doc = new RegDocument();
		doc.LoadFile(@"Resources/Redundant.reg");
		doc.Import();
		return doc;
	}

	[Benchmark]
	public void Regedit()
	{
		SharedTools.Import(@"Resources/Redundant.reg");
	}
}
