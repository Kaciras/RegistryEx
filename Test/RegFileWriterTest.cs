using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegistryEx.Test;

[TestClass]
public sealed class RegFileWriterTest
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
	public void EscapeString()
	{
		var stream = new MemoryStream();
		using (var writer = new RegFileWriter(stream))
		{
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.SetValue("a\"b\"", "a\"b\"");
			writer.SetValue(@"te\st", @"te\st");
		}
		AssertMatchRegistrySnapshot(stream);
	}

	[TestMethod]
	public void SetValue()
	{
		var stream = new MemoryStream();
		using(var writer = new RegFileWriter(stream))
		{
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.SetValue("String", "注册表文件使用 UTF16 编码");
			writer.SetValue("Long", 0xABCDEF987654321L);
			writer.SetValue("Int", 44846);
			writer.SetValue("Bytes", new byte[100]);
			writer.SetValue("Multi", new string[] { "foo", "", "bar" });
		}
		AssertMatchRegistrySnapshot(stream);
	}

	[ExpectedException(typeof(ArgumentException))]
	[TestMethod]
	public void SetValueWithInvalidKind()
	{
		using var writer = new RegFileWriter(Stream.Null);
		writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
		writer.SetValue("", 1, RegistryValueKind.Unknown);
	}

	[DataRow(1, RegistryValueKind.None)]
	[DataRow(1, RegistryValueKind.String)]
	[DataRow(1, RegistryValueKind.ExpandString)]
	[DataRow(1, RegistryValueKind.Binary)]
	[DataRow(1, RegistryValueKind.MultiString)]
	[DataRow("foo", RegistryValueKind.DWord)]
	[DataRow("foo", RegistryValueKind.QWord)]
	[DataRow("foo", RegistryValueKind.None)]
	[ExpectedException(typeof(InvalidCastException))]
	[DataTestMethod]
	public void SetValueWithIncorrectKind(object value, RegistryValueKind kind)
	{
		using var writer = new RegFileWriter(Stream.Null);
		writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
		writer.SetValue("SomeInvalidValue", value, kind);
	}

	[TestMethod]
	public void SetValueWithKind()
	{
		var stream = new MemoryStream();
		using (var writer = new RegFileWriter(stream))
		{
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.SetValue("Expand", "%windir%", RegistryValueKind.ExpandString);
			writer.SetValue("QWord", 1, RegistryValueKind.QWord);
			writer.SetValue("None", new byte[50], RegistryValueKind.None);
		}
		AssertMatchRegistrySnapshot(stream);
	}
	  
	[TestMethod]
	public void SetAndDeleteKey()
	{
		var stream = new MemoryStream();
		using (var writer = new RegFileWriter(stream))
		{
			writer.SetKey("HKEY_CURRENT_USER\\_A/B\"C_");

			writer.DeleteKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
		}
		AssertMatchRegistrySnapshot(stream);
	}

	[TestMethod]
	public void DeleteValue()
	{
		var stream = new MemoryStream();
		using (var writer = new RegFileWriter(stream))
		{
			writer.SetKey(@"HKEY_CURRENT_USER\_RH_Test_");
			writer.DeleteValue("@");
			writer.DeleteValue("SomeValue");
			writer.DeleteValue(string.Empty);
		}
		AssertMatchRegistrySnapshot(stream);
	}
}
