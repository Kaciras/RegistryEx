using System.Text;

namespace RegistryEx.Test;

[TestClass]
internal static class Snapshots
{
	static TestContext context = null!;

	[AssemblyInitialize]
	public static void Setup(TestContext ctx) => context = ctx;

	public static void AssertMatchRegFile(MemoryStream stream)
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
			if (Environment.GetEnvironmentVariable("CI") == null)
			{
				File.WriteAllBytes(path, stream.ToArray());
			}
			else
			{
				throw new AssertFailedException("Missing snapshot");
			}
		}
	}
}
