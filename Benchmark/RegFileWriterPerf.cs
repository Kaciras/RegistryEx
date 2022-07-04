using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Win32;

namespace RegistryEx.Benchmark;

public class RegFileWriterPerf
{
	[Benchmark]
	public RegFileWriter Write()
	{
		using var writer = new RegFileWriter(Stream.Null);
		writer.DeleteKey(@"HKEY_CURRENT_USER\_RH_Test_");
		writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
		writer.DeleteValue("Deleted");
		writer.SetValue("", @"%windir%\SecurityHealthSystray.exe", RegistryValueKind.ExpandString);
		writer.SetValue("String", "注册表文件使用 UTF16 编码");
		writer.SetValue("Long", 0xABCDEF987654321L);
		writer.SetValue("Int", 44846);
		writer.SetValue("Bytes", new byte[100]);
		writer.SetValue("None", new byte[100], RegistryValueKind.None);
		writer.SetValue("Multi", new string[] { "foo", "", "bar" });
		return writer;
	}
}
