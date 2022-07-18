using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RegistryEx;

internal static class InternalUtils
{
	/// <summary>
	/// Add missing deconstruct support for KeyValuePair.
	/// This method is implemented in .NET Standard 2.1.
	/// </summary>
	public static void Deconstruct<T1, T2>(
		this KeyValuePair<T1, T2> tuple,
		out T1 key,
		out T2 value)
	{
		key = tuple.Key;
		value = tuple.Value;
	}

	public static void AddAll<TKey, TValue>(
		this IDictionary<TKey, TValue> first,
		IDictionary<TKey, TValue> second)
	{
		foreach (var item in second)
			first.Add(item.Key, item.Value);
	}
}
