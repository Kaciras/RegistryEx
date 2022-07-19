using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace RegistryEx.Test;

[TestClass]
public sealed class RegHelperTest
{
	[TestCleanup]
	public void Cleanup()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}

	[DataRow("OpenKey", @"INVALID_ROOT\foobar", false)]
	[DataRow("KeyExists", @"INVALID_ROOT\foobar")]
	[DataRow("CreateKey", @"INVALID_ROOT\foobar")]
	[DataRow("DeleteKeyTree", @"INVALID_ROOT\foobar", false)]
	[DataRow("SetValue", @"INVALID_ROOT\foobar", 11, RegistryValueKind.DWord)]
	[DataRow("GetValue", @"INVALID_ROOT\foobar", 11)]
	[DataRow("GetValueKind", @"INVALID_ROOT\foobar")]
	[DataRow("DeleteValue", @"INVALID_ROOT\foobar", false)]
	[ExpectedException(typeof(ArgumentException))]
	[DataTestMethod]
	public void InvalidBasekey(string method, params object[] args)
	{
		var types = args.Select(v => v.GetType()).ToArray();
		var m = typeof(RegistryHelper).GetMethod(method, types);
		Assert.IsNotNull(m);

		try
		{
			m.Invoke(null, args);
		}
		catch (TargetInvocationException e)
		{
			throw e.InnerException!;
		}
	}

	[TestMethod]
	public void OpenKeyNonExists()
	{
		Assert.IsNull(RegistryHelper.OpenKey(@"HKCU\_NOE_\_NOE_"));
	}

	[DataRow(@"HKEY_CURRENT_USER", "HKEY_CURRENT_USER")]
	[DataRow(@"HKCU", "HKEY_CURRENT_USER")]
	[DataRow(@"HKCU\Software", @"HKEY_CURRENT_USER\Software")]
	[DataTestMethod]
	public void OpenKey(string path, string keyName)
	{
		using var key = RegistryHelper.OpenKey(path);
		Assert.AreEqual(keyName, key?.Name);
	}

	[DataRow(@"HKCU\_RH_Test_", false)]
	[DataRow(@"HKCC\System", true)]
	[DataTestMethod]
	public void KeyExists(string path, bool expected)
	{
		Assert.AreEqual(expected, RegistryHelper.KeyExists(path));
	}

	[TestMethod]
	public void CreateKey()
	{
		using var created = RegistryHelper.CreateKey(@"HKCU\_RH_Test_");
		using var key = Registry.CurrentUser.OpenSubKey("_RH_Test_");

		Assert.IsNotNull(key);
		Assert.AreEqual(created.Name, key.Name);
	}

	[TestMethod]
	public void DeleteKeyTree()
	{
		TestFixture.Import("SubKey");

		RegistryHelper.DeleteKeyTree(@"HKCU\_RH_Test_");
		Assert.IsNull(Registry.CurrentUser.OpenSubKey("_RH_Test_"));

		RegistryHelper.DeleteKeyTree(@"HKCU\_RH_Test_", false);
	}

	[ExpectedException(typeof(ArgumentException))]
	[TestMethod]
	public void DeleteNonExistsKeyTree()
	{
		RegistryHelper.DeleteKeyTree(@"HKCU\_RH_Test_");
	}

	[TestMethod]
	public void DeleteIgnoreNonExistsKeyTree()
	{
		RegistryHelper.DeleteKeyTree(@"HKCU\_RH_Test_", false);
	}

	[TestMethod]
	public void SetValue()
	{
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_");
		RegistryHelper.SetValue(@"HKCU\_RH_Test_\", 123, RegistryValueKind.QWord);
		Assert.AreEqual(123L, key.GetValue(""));
	}

	[TestMethod]
	public void SetValueAutoCreateKey()
	{
		RegistryHelper.SetValue(@"HKCU\_RH_Test_\Sub\", 123, RegistryValueKind.QWord);
		Assert.AreEqual(123L, Registry.GetValue(@"HKEY_CURRENT_USER\_RH_Test_\Sub", "", null));
	}

	[DataRow(@"HKCU\_NOE_\name")]
	[DataRow(@"HKCU\")]
	[DataRow(@"HKCU\name")]
	[DataTestMethod]
	public void GetNonExistsValue(string path)
	{
		Assert.IsNull(RegistryHelper.GetValue(path));
	}

	[TestMethod]
	public void GetValue()
	{
		TestFixture.Import("Kinds");
		Assert.AreEqual("文字文字", RegistryHelper.GetValue(@"HKCU\_RH_Test_\"));
		Assert.AreEqual(0x123, RegistryHelper.GetValue(@"HKCU\_RH_Test_\Dword"));
	}

	[TestMethod]
	public void GetValueWithOptions()
	{
		TestFixture.Import("Kinds");
		var got = RegistryHelper.GetValue(@"HKCU\_RH_Test_\Expand", RegistryValueOptions.DoNotExpandEnvironmentNames);
		Assert.AreEqual("%USERPROFILE%", got);
	}

	[DataRow(@"HKCU\_NOE_KEY_\Noe_Value")]
	[DataRow(@"HKCU\")]
	[DataRow(@"HKCU\Noe_Value")]
	[ExpectedException(typeof(IOException))]
	[DataTestMethod]
	public void GetNonExistValueKind(string path)
	{
		RegistryHelper.GetValueKind(path);
	}

	[DataRow(@"HKCU\_RH_Test_\", RegistryValueKind.String)]
	[DataRow(@"HKCU\_RH_Test_\None", RegistryValueKind.None)]
	[DataTestMethod]
	public void GetValueKind(string path, RegistryValueKind expected)
	{
		TestFixture.Import("Kinds");
		Assert.AreEqual(expected, RegistryHelper.GetValueKind(path));
	}

	[DataRow(@"HKCU\_NOE_KEY_\")]
	[DataRow(@"HKCU\Noe_Value")]
	[ExpectedException(typeof(IOException))]
	[DataTestMethod]
	public void DeleteNonExistValue(string path)
	{
		RegistryHelper.DeleteValue(path);
	}

	public void DeleteNonExistValueNotThrow()
	{
		RegistryHelper.DeleteValue(@"HKCU\_NOE_KEY_\", false);
	}

	[TestMethod]
	public void DeleteValue()
	{
		TestFixture.Import("Kinds");
		RegistryHelper.DeleteValue(@"HKCU\_RH_Test_\");

		using var key = Registry.CurrentUser.OpenSubKey("_RH_Test_");
		Assert.IsNull(key!.GetValue(""));
	}

	[ExpectedException(typeof(DirectoryNotFoundException))]
	[TestMethod]
	public void GetCLSIDValueException()
	{
		RegistryHelper.GetCLSIDValue("{66666666-0000-0000-6666-000000000000}");
	}

	[TestMethod]
	public void GetCLSIDValue()
	{
		var value = RegistryHelper.GetCLSIDValue("{C7657C4A-9F68-40fa-A4DF-96BC08EB3551}");
		Assert.AreEqual("Photo Thumbnail Provider", value);
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void LoadAppHiveLocked()
	{
		using var key = RegistryHelper.LoadAppHive("LoadTest.hiv", RegistryRights.FullControl, true);
		using var never = RegistryHelper.LoadAppHive("LoadTest.hiv", RegistryRights.FullControl, true);
	}

	[TestMethod]
	public void LoadAppHive()
	{
		File.Copy(@"Resources/Kinds.hiv", "LoadTest.hiv", true);

		using var key1 = RegistryHelper.LoadAppHive("LoadTest.hiv", RegistryRights.FullControl, false);
		using var key2 = RegistryHelper.LoadAppHive("LoadTest.hiv", RegistryRights.FullControl, false);
		key1.SetValue("Dword", 8964);
		key1.SetValue("Padding", new byte[8192]);

		Assert.AreEqual(8964, key2.GetValue("Dword"));
		Assert.IsTrue(new FileInfo("LoadTest.hiv").Length > 8192);
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void ElevateNonExists()
	{
		RegistryHelper.Elevate(Registry.CurrentUser, "_NON_EXISTS");
	}

	[TestMethod]
	public void Elevate()
	{
		using var key = Registry.CurrentUser.CreateSubKey("_test_sec_0");
		RegistryHelper.AddTokenPrivileges();

		var security = new RegistrySecurity();
		security.SetAccessRuleProtection(true, false);
		key.SetAccessControl(security);

		static void OpenWrite()
		{
			Registry.CurrentUser.OpenSubKey("_test_sec_0", true)!.Dispose();
		}

		Assert.ThrowsException<SecurityException>(OpenWrite);
		using (var _ = RegistryHelper.Elevate(Registry.CurrentUser, "_test_sec_0"))
		{
			OpenWrite();
		}
		Assert.ThrowsException<SecurityException>(OpenWrite);

		security.SetAccessRuleProtection(false, true);
		key.SetAccessControl(security);
		Registry.CurrentUser.DeleteSubKey("_test_sec_0");
	}
}
