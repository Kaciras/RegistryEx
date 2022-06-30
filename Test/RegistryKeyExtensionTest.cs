namespace RegistryHelper.Test;

[TestClass]
public sealed class RegistryKeyExtensionTest
{
	[TestMethod]
	public void Exists()
	{
		using var key = Registry.CurrentUser.OpenSubKey("Software");
		Assert.IsTrue(key!.Exists());
	}

	[TestMethod]
	public void NotExists()
	{
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_");
		Registry.CurrentUser.DeleteSubKey("_RH_Test_");
		Assert.IsFalse(key.Exists());
	}

	[TestMethod]
	public void Delete()
	{
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_");
		key.Delete();
		Assert.IsNull(Registry.CurrentUser.OpenSubKey("_RH_Test_"));
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void DeleteNonExists()
	{
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_");
		key.Delete();
		key.Delete();
	}

	[DataRow("_NON_EXISTS_", false)]
	[DataRow("Software", true)]
	[DataTestMethod]
	public void ContainsSubKey(string keyName, bool expected)
	{
		Assert.AreEqual(expected, Registry.CurrentUser.ContainsSubKey(keyName));
	}

	[TestMethod]
	public void GetValueKind()
	{
		Assert.AreEqual(RegistryValueKind.ExpandString, Registry.CurrentUser.GetValueKind("Environment", "Path"));
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void GetValueKindNonExistKey()
	{
		Registry.CurrentUser.GetValueKind("_NOE_", "Path");
	}

	[ExpectedException(typeof(IOException))]
	[TestMethod]
	public void GetValueKindNonExists()
	{
		Registry.CurrentUser.GetValueKind("Environment", "_NOE_");
	}

	[ExpectedException(typeof(InvalidCastException))]
	[TestMethod]
	public void GetValue()
	{
		Registry.CurrentUser.GetValue<int>("Environment", "Path");
	}
}
