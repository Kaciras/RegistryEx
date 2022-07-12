using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistryEx.Test;

[TestClass]
internal static class Snapshots
{
	static TestContext context = null!;

	[AssemblyInitialize]
	public static void Setup(TestContext ctx) => context = ctx;

	public static void AssertMatchRegistrySnapshot(MemoryStream stream)
	{
		var className = context.FullyQualifiedTestClassName;
		var i = className.LastIndexOf(".");
		if (i != -1)
		{
			className = className.Substring(i + 1);
		}

		var directory = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"../../..",
			"Snapshots",
			className
		);
		Directory.CreateDirectory(directory);

		var path = Path.Combine(directory, context.TestName + ".reg");
		try
		{
			var expected = File.ReadAllText(path, Encoding.Unicode);
			var actual = Encoding.Unicode.GetString(stream.ToArray());

			//                        Skip BOM
			Assert.AreEqual(expected, actual.Substring(1));
		}
		catch (FileNotFoundException)
		{
			File.WriteAllBytes(path, stream.ToArray());
		}
	}
}
