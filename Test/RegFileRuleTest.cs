namespace RegistryEx.Test;

[TestClass]
public sealed class RegFileRuleTest
{
	[TestInitialize]
	public void ImportTestData()
	{
		RegistryHelper.Import(@"Resources\Kinds.reg");
	}

	[TestCleanup]
	public void Cleanup()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}

	[TestMethod]
	public void CheckNoNeeded()
	{
		Assert.IsFalse(RegistryHelper.IsTODO(@"Resources\Kinds.reg"));
	}

	[TestMethod]
	public void Check()
	{
		Assert.IsTrue(RegistryHelper.IsTODO(@"Resources\ImportTest.reg"));
	}
}
