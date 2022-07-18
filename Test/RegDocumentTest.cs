namespace RegistryEx.Test;

[TestClass]
public class RegDocumentTest
{
	[TestCleanup]
	public void Cleanup()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}

	[ExpectedException(typeof(ArgumentException))]
	[DataRow(@"foobar")]
	[DataRow(@"")]
	[DataRow(@"\")]
	[DataRow(@"\HKCU\foobar")]
	[DataRow(@"HKCU\foobar\")]
	[DataRow(@"HKCU\\foobar")]
	[DataTestMethod]
	public void CheckKeyName(string name)
	{
		new RegDocument().CreateKey(name);
	}

	[TestMethod]
	public void NormalizeKeyName()
	{
		var doc = new RegDocument();
		doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_");
		doc.DeleteKey(@"HKCU\_rh_tEST_");
		Assert.AreEqual(0, doc.Created.Count);
	}

	[TestMethod]
	public void CreateKey()
	{
		var doc = new RegDocument();
		var a = doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_");
		var b = doc.CreateKey(@"HKCU\_RH_Test_");
		var c = doc.CreateKey(@"HKCU\_rh_tEST_");

		Assert.AreSame(a, b);
		Assert.AreSame(b, c);
		Assert.AreEqual(1, doc.Created.Count);
	}

	[TestMethod]
	public void NoTrailingSlashOnBasekey()
	{
		var doc = new RegDocument();
		doc.CreateKey(@"HKCU");
		Assert.IsTrue(doc.Created.ContainsKey("HKEY_CURRENT_USER"));
	}

	[TestMethod]
	public void ParseFile()
	{
		var doc = RegDocument.ParseFile(@"Resources/Kinds.reg");
		Assert.AreEqual(1, doc.Created.Count);
	}

	[TestMethod]
	public void DeleteKey()
	{
		var doc = new RegDocument();
		doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_\foobar");
		doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_/foobar");
		doc.DeleteKey(@"HKEY_CURRENT_USER\_RH_Test_");

		CollectionAssert.AreEqual(new string[] { @"HKEY_CURRENT_USER\_RH_Test_/foobar" }, doc.Created.Keys);

		Assert.AreEqual(1, doc.Erased.Count);
		Assert.AreEqual(@"HKEY_CURRENT_USER\_RH_Test_", doc.Erased.First());
	}

	[TestMethod]
	public void Deterministic()
	{
		var doc = new RegDocument();
		doc.Load(Resources.Redundant);
		Assert.AreEqual(2, doc.Created.Count);
	}

	[TestMethod]
	public void IsSuitable()
	{
		var doc = new RegDocument();
		Assert.IsTrue(doc.IsSuitable);

		doc.Load(Resources.Redundant);
		Assert.IsFalse(doc.IsSuitable);

		TestFixture.Import("SubKey");
		Assert.IsTrue(doc.IsSuitable);

		using var key = Registry.CurrentUser.CreateSubKey(@"_RH_Test_\11");
		key.SetValue("foo", "bar");
		Assert.IsFalse(doc.IsSuitable);
	}

	[TestMethod]
	public void LoadRegistry()
	{
		TestFixture.Import("SubKey");
		var doc = new RegDocument();

		using var key = Registry.CurrentUser.OpenSubKey("_RH_Test_");
		doc.Load(key!);

		Assert.AreEqual(2, doc.Created.Count);
		Assert.AreEqual(
			new RegistryValue("baz", RegistryValueKind.String), 
			doc.Created[@"HKEY_CURRENT_USER\_RH_Test_"]["foo"]
		);
		Assert.AreEqual(
			new RegistryValue(0x44, RegistryValueKind.DWord),
			doc.Created[@"HKEY_CURRENT_USER\_RH_Test_\Sub"][""]
		);
	}

	[TestMethod]
	public void CreateRestorePoint()
	{
		TestFixture.Import("SubKey");

		var doc = new RegDocument();
		doc.DeleteKey(@"HKEY_CURRENT_USER\_RH_Test_");

		doc.CreateKey(@"HKEY_CURRENT_USER\_SomeKey_");

		var reverse = doc.CreateRestorePoint();
		Assert.AreEqual(1, reverse.Erased.Count);
	}

	[TestMethod]
	public void Import()
	{
		var doc = new RegDocument();
		doc.Load(Resources.SubKey);
		doc.Import();

		var doc2 = new RegDocument();
		doc.Load(Resources.SubKey);
		Assert.IsTrue(doc2.IsSuitable);
	}

	[TestMethod]
	public void Write()
	{
		var doc = new RegDocument();
		doc.DeleteKey(@"HKEY_CURRENT_USER\_RH_Test_");

		var key = doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_");
		key["Deleted"] = default;
		key["foo"] = new RegistryValue("baz", RegistryValueKind.String);
		key[""] = new RegistryValue(0x123, RegistryValueKind.DWord);

		var key2 = doc.CreateKey(@"HKEY_CURRENT_USER\_RH_Test_\Sub");
		key2[""] = new RegistryValue(0x123, RegistryValueKind.QWord);

		Snapshots.AssertMatchRegDocument(doc);
	}
}
