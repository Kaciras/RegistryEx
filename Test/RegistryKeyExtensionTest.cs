using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace RegistryEx.Test;

[TestClass]
public sealed class RegistryKeyExtensionTest
{
	readonly RegistryKey HKCU = Registry.CurrentUser;

	[TestInitialize]
	public void Setup()
	{
		SharedTools.Import(@"Resources/Kinds.reg");
	}

	[TestCleanup]
	public void Cleanup()
	{
		HKCU.DeleteSubKeyTree("_RH_Test_", false);
	}

	[TestMethod]
	public void Exists()
	{
		using var key = HKCU.OpenSubKey("_RH_Test_");
		Assert.IsTrue(key!.Exists());
	}

	[TestMethod]
	public void NotExists()
	{
		using var key = HKCU.CreateSubKey("_RH_Test_");
		HKCU.DeleteSubKey("_RH_Test_");
		Assert.IsFalse(key.Exists());
	}

	[TestMethod]
	public void Delete()
	{
		using var key = HKCU.CreateSubKey("_RH_Test_");
		key.Delete();
		Assert.IsNull(HKCU.OpenSubKey("_RH_Test_"));
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void DeleteNonExists()
	{
		using var key = HKCU.CreateSubKey("_RH_Test_");
		key.Delete();
		key.Delete();
	}

	[DataRow("_RH_Test_", true)]
	[DataRow("__NOE__", false)]
	[DataTestMethod]
	public void ContainsSubKey(string keyName, bool expected)
	{
		Assert.AreEqual(expected, HKCU.ContainsSubKey(keyName));
	}

	[TestMethod]
	public void GetValueKind()
	{
		var actual = HKCU.GetValueKind("_RH_Test_", "Expand");
		Assert.AreEqual(RegistryValueKind.ExpandString, actual);
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void GetValueKindNonExistKey()
	{
		HKCU.GetValueKind("__NOE__", "Path");
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void GetValueKindNonExists()
	{
		HKCU.GetValueKind("_RH_Test_", "__NOE__");
	}

	[ExpectedException(typeof(InvalidCastException))]
	[TestMethod]
	public void GetValueWithInvalidType()
	{
		HKCU.GetValue<int>("_RH_Test_", string.Empty);
	}

	[TestMethod]
	public void GetNonExistsValue()
	{
		Assert.AreEqual(0, HKCU.GetValue<int>("_RH_Test_", "__NOE__"));
	}

	[TestMethod]
	public void GetValueFromNonExistKey()
	{
		Assert.AreEqual(0, HKCU.GetValue<int>("__NOE__", string.Empty));
	}

	[TestMethod]
	public void GetValue()
	{
		Assert.AreEqual(0x123, HKCU.GetValue<int>("_RH_Test_", "Dword"));
	}

	[TestMethod]
	public void DeleteNonExistValue()
	{
		HKCU.DeleteValue("_RH_Test_", "__NOE__");
	}

	[TestMethod]
	public void DeleteValueOnNonExistKey()
	{
		HKCU.DeleteValue("__NOE__", "Dword");
	}

	[TestMethod]
	public void DeleteValue()
	{
		HKCU.DeleteValue("_RH_Test_", "Dword");

		Assert.IsNull(HKCU.GetValue("Dword"));
		Assert.IsTrue(HKCU.ContainsSubKey("_RH_Test_"));
	}

	[TestMethod]
	public void SetValueOnNonExistKey()
	{
		HKCU.SetValue(@"_RH_Test_\New", "test", 123);
		Assert.AreEqual(123, Registry.GetValue(@"HKEY_CURRENT_USER\_RH_Test_\New", "test", null));
	}

	[TestMethod]
	public void SetValue()
	{
		HKCU.SetValue(@"_RH_Test_", "Dword", 8964);
		Assert.AreEqual(8964, Registry.GetValue(@"HKEY_CURRENT_USER\_RH_Test_", "Dword", 0));
	}

	[TestMethod]
	public void SaveAndRestoreHive()
	{
		using var key = HKCU.OpenSubKey("_RH_Test_");
		key!.SaveHive("saved.hiv");

		using var sub = HKCU.CreateSubKey(@"_RH_Test_\Restored");
		sub!.RestoreHive(@"Resources/Kinds.hiv");

		Assert.AreEqual("文字文字", sub.GetValue(""));
	}

	[TestMethod]
	public void LoadHive()
	{
		File.Copy(@"Resources/Kinds.hiv", "LoadTest.hiv", true);

		Registry.LocalMachine.LoadHive("_RH_Test_", "LoadTest.hiv");
		using (var key = Registry.LocalMachine.OpenSubKey(@"_RH_Test_", true))
		{
			var padding = new byte[8192];
			new Random().NextBytes(padding);

			key!.SetValue("Padding", padding);
			Assert.AreEqual("文字文字", key.GetValue(""));
		}

		Registry.LocalMachine.UnLoadHive("_RH_Test_");
		Assert.IsTrue(new FileInfo("LoadTest.hiv").Length > 8192);
	}
}
