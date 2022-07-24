using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace RegistryEx.Test;

/// <summary>
/// A snapshot testing tool to simply assertion of `.reg` file result.
/// <br/>
/// Files are stored at {Project dir}/Snapshots/{Class}/{Method}-{Index}.reg
/// </summary>
internal static class Snapshots
{
	static MethodBase? latestMethod;
	static int index = 0;

	public static void AssertMatchRegFile(RegDocument document)
	{
		var stream = new MemoryStream();
		document.WriteTo(stream);
		AssertMatchRegFile(stream);
	}

	public static void AssertMatchRegFile(MemoryStream stream)
	{
		var method = GetTestMethod();
		var directory = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"../../..",
			"Snapshots",
			method.DeclaringType!.Name
		);
		Directory.CreateDirectory(directory);

		if (latestMethod != method)
		{
			index = 0;
			latestMethod = method;
		}

		var path = Path.Combine(directory, $"{method.Name}-{index}.reg");
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

	static MethodBase GetTestMethod()
	{
		foreach (var stackFrame in new StackTrace().GetFrames())
		{
			var methodBase = stackFrame.GetMethod()!;
			var attributes = methodBase.GetCustomAttributes(typeof(TestMethodAttribute), false);
			if (attributes.Length >= 1)
			{
				return methodBase;
			}
		}
		throw new InvalidOperationException("Snapshots.Assert* can only be called on test");
	}
}
