using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace RegistryEx.Test;

[TestClass]
public class KernelTransactionTest
{
	[TestInitialize]
	public void Setup()
	{
		SharedTools.Import(@"Resources/Kinds.reg");
	}

	[TestCleanup]
	public void Cleanup()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}

	[TestMethod]
	public void Rollback()
	{
		var transaction = new KernelTransaction();
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_", transaction);

		key.DeleteValue("Dword");
		Assert.IsNull(key.GetValue("Dword"));

		transaction.Rollback();
		Assert.IsNull(key.GetValue("Dword"));

		key.DeleteValue("Dword");
		Assert.AreEqual(0x123, RegistryHelper.GetValue(@"HKCU\_RH_Test_\Dword"));
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void ThrowWhenOperateWithDisposedTransaction()
	{
		var transaction = new KernelTransaction();
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_", transaction);
		transaction.Dispose();

		Assert.AreEqual(6, key.ValueCount);
	}

	[ExpectedException(typeof(Win32Exception))]
	[TestMethod]
	public void CommitDisposed()
	{
		var transaction = new KernelTransaction();
		transaction.Dispose();
		transaction.Commit();
	}

	[TestMethod]
	public void RollbackUncommitedOnDisposing()
	{
		using (var transaction = new KernelTransaction())
		{
			using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_", transaction);
			key.DeleteValue("Dword");
		}
		Assert.AreEqual(0x123, RegistryHelper.GetValue(@"HKCU\_RH_Test_\Dword"));
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void DeleteKeyWithoutTransaction()
	{
		using var transaction = new KernelTransaction();
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_", transaction);

		Registry.CurrentUser.DeleteSubKey("_RH_Test_");
		key.CreateSubKey("Sub", transaction);
	}

	[TestMethod]
	public void AbortOnNonTransactionOperation()
	{
		using var transaction = new KernelTransaction();
		Registry.CurrentUser.DeleteSubKey("_RH_Test_", transaction);

		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_", true);
		key.SetValue("foobar", 11);

		Assert.ThrowsException<InvalidOperationException>(transaction.Commit);
		Assert.IsTrue(Registry.CurrentUser.ContainsSubKey("_RH_Test_"));
	}
}
