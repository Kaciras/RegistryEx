using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Win32;

namespace RegistryEx;

/// <summary>
/// This struct support Equals & GetHashCode, but it is not immutatable if you construct 
/// with a mutatable value (e.g. byte[]).
/// </summary>
public readonly struct RegistryValue : IEquatable<RegistryValue>
{
	public static readonly RegistryValue DELETED = default;

	public bool IsDelete => Kind == RegistryValueKind.Unknown;

	public readonly object Value { get; }

	public readonly RegistryValueKind Kind { get; }

	[SuppressMessage("Style", "IDE0066")]
	public RegistryValue(object value, RegistryValueKind kind)
	{
		Kind = kind;

		// The standard library use int for DWORD and long for QWORD.
		// https://github.com/dotnet/runtime/blob/cbfc5499e6024a0d53d7c9957c143cf45fac2987/src/libraries/Microsoft.Win32.Registry/src/Microsoft/Win32/RegistryKey.Windows.cs#L838
		try
		{
			switch (kind, value)
			{
				case (RegistryValueKind.Unknown, _):
					throw new ArgumentException("Unknown is not a valid registry value kind");
				case (RegistryValueKind.Binary, byte[]):
				case (RegistryValueKind.None, byte[]):
				case (RegistryValueKind.MultiString, string[]):
					Value = value;
					break;
				case (RegistryValueKind.ExpandString, _):
				case (RegistryValueKind.String, _):
					Value = value.ToString();
					break;
				case (RegistryValueKind.DWord, _):
					Value = Convert.ToUInt32(value);
					break;
				case (RegistryValueKind.QWord, _):
					Value = Convert.ToUInt64(value);
					break;
				default:
					throw new ArgumentException("The type of the value object did not match the kind");
			}
		}
		catch (FormatException)
		{
			throw new ArgumentException($"Cannot convert ${value} to kind: {kind}");
		}
		catch (Exception e) when (e is InvalidCastException || e is NullReferenceException)
		{
			throw new ArgumentException("The type of the value object did not match the kind");
		}
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
