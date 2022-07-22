using System;
using System.Linq;
using Microsoft.Win32;

namespace RegistryEx;

public readonly struct RegistryValue : IEquatable<RegistryValue>
{
	public static readonly RegistryValue DELETED = default;

	public bool IsDelete => Kind == RegistryValueKind.Unknown;

	public readonly object Value { get; }

	public readonly RegistryValueKind Kind { get; }

	public RegistryValue(object value, RegistryValueKind kind)
	{
		Value = value;
		Kind = kind;
	}

	public void Deconstruct(out object value, out RegistryValueKind kind)
	{
		kind = Kind;
		value = Value;
	}

	public override bool Equals(object obj)
	{
		return obj is RegistryValue v && Equals(v);
	}

	public bool Equals(RegistryValue other)
	{
		return Kind == other.Kind && Kind switch
		{
			RegistryValueKind.Unknown => true,
			RegistryValueKind.MultiString => ArrayEquals<string>(other.Value),
			RegistryValueKind.Binary or
			RegistryValueKind.None => ArrayEquals<byte>(other.Value),
			_ => Value.Equals(other.Value),
		};
	}

	bool ArrayEquals<T>(object otherValue)
	{
		if (otherValue is not T[] || otherValue == null)
		{
			return false;
		}
		return ((T[])Value).SequenceEqual((T[])otherValue);
	}

	public override int GetHashCode() => (int)Kind ^ Kind switch
	{
		RegistryValueKind.Unknown => 0,
		RegistryValueKind.MultiString => ArrayHashCode((string[])Value),
		RegistryValueKind.Binary or
		RegistryValueKind.None => ArrayHashCode((byte[])Value),
		_ => Value.GetHashCode(),
	};

	int ArrayHashCode<T>(T[] values)
	{
		return values.Aggregate(values.Length, (s, v) => unchecked(s * 31 + v!.GetHashCode()));
	}

	public override string ToString()
	{
		return $"{Kind}:{Value}";
	}

	public static RegistryValue From(RegistryKey key, string name)
	{
		var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
		return value == null ? DELETED : new(value, key.GetValueKind(name));
	}
}
