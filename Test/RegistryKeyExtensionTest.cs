using System.Security.Principal;
using System.Transactions;
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
	public void CopyTree()
	{
		var user = WindowsIdentity.GetCurrent().User!.Value;
		using var hkcuSource = Registry.Users.OpenSubKey($@"{user}\_RH_Test_", true)!;
		using var branchA = hkcuSource.CreateSubKey("A", true);

		HKCU.CopyTree("_RH_Test_", branchA);

		using var leaf = branchA.OpenSubKey(@"A")!;
		Assert.AreEqual(0, leaf.SubKeyCount);
		Assert.AreEqual(0x123, branchA.GetValue("Dword"));
	}

	[TestMethod]
	public void OpenSubKeyTransacted()
	{
		using var transaction = new KernelTransaction();
		using var key = HKCU.OpenSubKey("_RH_Test_", transaction, true);

		key!.DeleteValue("Dword");
		Assert.AreEqual(0x123, RegistryHelper.GetValue(@"HKCU\_RH_Test_\Dword"));

		transaction.Commit();
		Assert.IsNull(RegistryHelper.GetValue(@"HKCU\_RH_Test_\Dword"));
	}

	[ExpectedException(typeof(UnauthorizedAccessException))]
	[TestMethod]
	public void OpenSubKeyTransactedReadonly()
	{
		using var transaction = new KernelTransaction();
		using var key = HKCU.OpenSubKey("_RH_Test_", transaction);
		key!.SetValue("Dword", 0x456);
	}

	[TestMethod]
	public void OpenSubKeyTransactedNonExists()
	{
		using var transaction = new KernelTransaction();
		Assert.IsNull(HKCU.OpenSubKey("_NOE_", transaction));
	}

	[TestMethod]
	public void DeleteSubKeyTransacted()
	{
		using var transaction = new KernelTransaction();
		HKCU.DeleteSubKey("_RH_Test_", transaction);

		Assert.IsTrue(RegistryHelper.KeyExists(@"HKCU\_RH_Test_"));
		transaction.Commit();
		Assert.IsFalse(RegistryHelper.KeyExists(@"HKCU\_RH_Test_"));
	}

	[ExpectedException(typeof(ArgumentException))]
	[TestMethod]
	public void DeleteSubKeyTransactedThrowOnMissing()
	{
		using var transaction = new KernelTransaction();
		HKCU.DeleteSubKey("_NOE_", transaction);
	}

	[TestMethod]
	public void DeleteSubKeyTransactedAllowMissing()
	{
		using var transaction = new KernelTransaction();
		HKCU.DeleteSubKey("_NOE_", transaction, false);
	}

	[ExpectedException(typeof(InvalidOperationException))]
	[TestMethod]
	public void DeleteSubKeyNotEmpty()
	{
		using var _ = TestFixture.Import("SubKey");
		using var transaction = new KernelTransaction();

		HKCU.DeleteSubKey("_RH_Test_", transaction);
	}

	[TestMethod]
	public void DeleteSubKeyTreeTransacted()
	{
		using var _ = TestFixture.Import("SubKey");
		using var transaction = new KernelTransaction();
		
		HKCU.DeleteSubKeyTree("_RH_Test_", transaction);
		Assert.IsTrue(HKCU.ContainsSubKey("_RH_Test_"));

		transaction.Commit();
		Assert.IsFalse(HKCU.ContainsSubKey("_RH_Test_"));
	}

	[ExpectedException(typeof(ArgumentException))]
	[TestMethod]
	public void DeleteSubKeyTreeTransactedThrowOnMissing()
	{
		using var transaction = new KernelTransaction();
		HKCU.DeleteSubKeyTree("_NOE_", transaction);
	}

	[TestMethod]
	public void DeleteSubKeyTreeTransactedAllowMissing()
	{
		using var transaction = new KernelTransaction();
		HKCU.DeleteSubKeyTree("_NOE_", transaction, false);
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
