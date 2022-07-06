using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace RegistryEx.Test;

[TestClass]
public class RegDocumentTest
{
	[TestMethod]
	public void MyTestMethod()
	{
		var doc = RegDocument.ParseFile(@"Resources/Kinds.reg");
		Assert.AreEqual(1, doc.Count);
	}

	[TestMethod]
	public void Deterministic()
	{
		var doc = new RegDocument();
		doc.Load(@"Resources/Redundant.reg");
		Assert.AreEqual(1, doc.Count);
	}
}
