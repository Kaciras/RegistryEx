using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegistryEx.Test;

[TestClass]
public sealed class RegistryKeyExtensionTest
{
	static readonly RegistryKey HKCU = Registry.CurrentUser;

	[TestInitialize]
	public void Setup()
	{
		SharedTools.Import(@"Resources/Kinds.reg");
	}

	[TestCleanup]
	public void Cleanup()
	{
		HKCU.DeleteSubKeyTree("_RH_Test_", false);
		HKCU.DeleteSubKeyTree("_Renamed_", false);
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

	[ExpectedException(typeof(UnauthorizedAccessException))]
	[TestMethod]
	public void RenameWithoutPermission()
	{
		using var key = HKCU.OpenSubKey("_RH_Test_")!;
		key.Rename("_Renamed_");
	}

	[TestMethod]
	public void Rename()
	{
		using var _ = TestFixture.Import("SubKey");
		HKCU.Rename("_RH_Test_", "_Renamed_");
		using var key = HKCU.OpenSubKey(@"_Renamed_\Sub");

		Assert.IsFalse(HKCU.ContainsSubKey("_RH_Test_"));
		Assert.AreEqual(0x44, key!.GetValue(""));
	}

	[TestMethod]
	public void RenameCurrent()
	{
		using var key = HKCU.OpenSubKey("_RH_Test_", true)!;
		key.Rename("_Renamed_");

		Assert.IsFalse(HKCU.ContainsSubKey("_RH_Test_"));
		Assert.AreEqual("文字文字", key!.GetValue(""));
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

	[Timeout(1000)]
	[TestMethod]
	public void WaitForChange()
	{
		var DelaySet = (int value) => Task.Run(async () =>
		{
			using var key = HKCU.CreateSubKey("_RH_Test_");
			await Task.Delay(100);
			key.SetValue("Dword", value);
		});

		using var key = HKCU.CreateSubKey("_RH_Test_");

		DelaySet(0x7777);
		key.WaitForChange(RegNotifyFilter.LAST_SET, false);
		Assert.AreEqual(0x7777, key.GetValue("Dword"));

		DelaySet(0x8888);
		key.WaitForChange(RegNotifyFilter.LAST_SET, false);
		Assert.AreEqual(0x8888, key.GetValue("Dword"));
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
