using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegistryEx.Test;

[TestClass]
public class RegFileWriterTest
{
	public TestContext TestContext { get; set; }

	void AssertMatchSnapshot(MemoryStream stream)
	{
		var src = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../..");
		var directory = Path.Combine(src, "Snapshots", "RegFileWriterTest");
		Directory.CreateDirectory(directory);

		var path = Path.Combine(directory, TestContext.TestName + ".reg");
		try
		{
			var expected = File.ReadAllText(path, Encoding.Unicode);
			Assert.AreEqual(expected, Encoding.Unicode.GetString(stream.ToArray()));
		}
		catch(FileNotFoundException)
		{
			File.WriteAllBytes(path, stream.ToArray());
		}
	}

	[TestMethod]
	public void MyTestMethod()
	{
		var stream = new MemoryStream();

		using(var writer = new RegFileWriter(stream))
		{
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.SetValue("", "注册表文件使用 UTF16 编码");
		}

		AssertMatchSnapshot(stream);
	}
}
