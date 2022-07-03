using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegistryEx.Test;

[TestClass]
public class RegFileWriterTest
{
	public TestContext TestContext { get; set; }

	void AssertMatchRegistrySnapshot(MemoryStream stream)
	{
		var directory = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"../../..",
			"Snapshots",
			"RegFileWriterTest"
		);
		Directory.CreateDirectory(directory);

		var path = Path.Combine(directory, TestContext.TestName + ".reg");
		try
		{
			var expected = File.ReadAllText(path, Encoding.Unicode);
			var actual = Encoding.Unicode.GetString(stream.ToArray());

			//                         Skip BOM
			Assert.AreEqual(expected, actual[1..]);
		}
		catch (FileNotFoundException)
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

			var binary = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
			writer.SetValue("BinaryHKEY_CURRENT_USER", binary);

			writer.SetValue("", "注册表文件使用 UTF16 编码");
		}
		AssertMatchRegistrySnapshot(stream);
	}
}
