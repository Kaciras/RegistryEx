namespace RegistryEx.Test;

[TestClass]
public sealed class RegistryValueTest
{
	readonly RegistryValue[] values = new RegistryValue[]
	{
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
	};

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
		Assert.AreNotEqual(value, "");
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
}
