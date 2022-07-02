using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace RegistryHelper;

public class RegDocument
{
	internal const string VER_LINE = "Windows Registry Editor Version 5.00";

	
	public void WriteTo(string file)
	{
		using var writer = new StreamWriter(File.OpenWrite(file), Encoding.Unicode);
		writer.WriteLine(VER_LINE);


	}
}
