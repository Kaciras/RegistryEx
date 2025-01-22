using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace RegistryEx.Test;

[TestClass]
public sealed class RegistryValueTest
{
	readonly RegistryValue[] values =
	[
		RegistryValue.DELETED,
		new(Array.Empty<byte>(), RegistryValueKind.None),
		new(Array.Empty<byte>(), RegistryValueKind.Binary),
		new(new byte[1], RegistryValueKind.Binary),
		new(new byte[]{ 1 }, RegistryValueKind.Binary),
		new(0L, RegistryValueKind.QWord),
		new(1L, RegistryValueKind.QWord),
		new(0, RegistryValueKind.DWord),
		new("", RegistryValueKind.ExpandString),
		new("", RegistryValueKind.String),
		new(Array.Empty<string>(), RegistryValueKind.MultiString),
	];

	public static IEnumerable<object?[]> InvalidConstructArgs()
	{
		yield return new object?[] { 123, RegistryValueKind.Unknown };
		yield return new object?[] { DateTime.Now, RegistryValueKind.DWord };
		yield return new object?[] { "foobar", RegistryValueKind.DWord };
		yield return new object?[] { "", RegistryValueKind.QWord };
		yield return new object?[] { "", RegistryValueKind.None };
		yield return new object?[] { null, RegistryValueKind.None };
		yield return new object?[] { "", RegistryValueKind.MultiString };
		yield return new object?[] { null, RegistryValueKind.String };
		yield return new object?[] { new byte[1], RegistryValueKind.MultiString };
	}

	[ExpectedException(typeof(ArgumentException))]
	[DynamicData(nameof(InvalidConstructArgs), DynamicDataSourceType.Method)]
	[DataTestMethod]
	public void Construct(object value, RegistryValueKind kind)
	{
		new RegistryValue(value, kind);
	}

	public static IEnumerable<object[]> DifferentValues()
	{
		var values = new RegistryValueTest().values;
		var length = values.Length;

		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length; j++)
			{
				if (i != j)
				{
					yield return new object[] { values[i], values[j] };
				}
			}
		}
	}

	public static IEnumerable<object[]> EqualValues()
	{
		var values0 = new RegistryValueTest().values;
		var values1 = new RegistryValueTest().values;
		var length = values0.Length;

		for (int i = 0; i < length; i++)
		{
			yield return new object[] { values0[i], values1[i] };
		}
	}

	[TestMethod]
	public void EqualityDefferentType()
	{
		var value = new RegistryValue(0, RegistryValueKind.DWord);
		Assert.IsFalse(value.Equals(""));
		Assert.AreNotEqual(value.GetHashCode(), "".GetHashCode());
	}

	[DynamicData(nameof(EqualValues), DynamicDataSourceType.Method)]
	[DataTestMethod]
	public void Equality0(RegistryValue a, RegistryValue b)
	{
		Assert.AreEqual<object>(a, b);
		Assert.AreEqual(a, b);
		Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[DynamicData(nameof(DifferentValues), DynamicDataSourceType.Method)]
	[DataTestMethod]
	public void Equality1(RegistryValue a, RegistryValue b)
	{
		Assert.AreNotEqual<object>(a, b);
		Assert.AreNotEqual(a, b);
		Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
	}

	public static IEnumerable<object?[]> Kinds()
	{
		yield return new object?[] { "_NOE_", null, RegistryValueKind.Unknown };
		yield return new object?[] { "", "文字文字", RegistryValueKind.String };
		yield return new object?[] { "Dword", 0x123, RegistryValueKind.DWord };
		yield return new object?[] { "Qword", 0x666888L, RegistryValueKind.QWord };
		yield return new object?[] { "Expand", "%USERPROFILE%", RegistryValueKind.ExpandString };
		yield return new object?[] { "Binary", new byte[] { 0xfa, 0x51, 0x6f, 0x89 }, RegistryValueKind.Binary };
		yield return new object?[] { "None", new byte[] { 0x19, 0x89, 0x06, 0x04, 0 }, RegistryValueKind.None };
		yield return new object?[] { "Multi", new string[] { "Str0","Str1" }, RegistryValueKind.MultiString };
	}

	[DynamicData(nameof(Kinds), DynamicDataSourceType.Method)]
	[DataTestMethod]
	public void From(string name, object? value, RegistryValueKind kind)
	{
		using var _ = TestFixture.Import("Kinds");
		using var key = Registry.CurrentUser.CreateSubKey("_RH_Test_");

		var fromRegistry = RegistryValue.From(key, name);
		Assert.AreEqual(kind, fromRegistry.Kind);

		if (value is Array data)
		{
			CollectionAssert.AreEqual(data,
				(System.Collections.ICollection)fromRegistry.Value);
		}
		else
		{
			Assert.AreEqual(value, fromRegistry.Value);
		}
	}
}
