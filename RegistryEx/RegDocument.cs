using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace RegistryEx;

public class RegDocument : Dictionary<string, Dictionary<string, PlainRegistryValue>>
{
	internal const string VER_LINE = "Windows Registry Editor Version 5.00";

	public HashSet<string> Erased { get; } = new();

	public void DeleteKey(string keyName)
	{
		var s = keyName.IndexOf('\\') + 1;
		if (s == 0 || s == keyName.Length)
		{
			throw new ArgumentException("Can not delete root key");
		}

		Erased.Add(keyName);

		foreach (var existing in Keys)
		{
			if (existing.StartsWith(keyName))
				Remove(existing);
		}
	}

	public void Load(string file)
	{
		var currentKeyName = "";
		Dictionary<string, PlainRegistryValue> currentKey = null;
		var reader = RegFileReader.OpenFile(file);

		while (reader.Read())
		{
			if (reader.IsKey)
			{
				if (currentKeyName != reader.Key)
				{
					currentKeyName = reader.Key;
				}
				if (reader.IsDelete)
				{
					DeleteKey(reader.Key);
				}
				else if (TryGetValue(currentKeyName, out var e))
				{
					currentKey = e;
				}
				else
				{
					currentKey = new();
					this[currentKeyName] = currentKey;
				}
			}
			else
			{
				if (reader.IsDelete)
				{
					currentKey[reader.Name] = new PlainRegistryValue(null, 0);
				}
				else
				{
					currentKey[reader.Name] = new PlainRegistryValue(reader.Value, reader.Kind);
				}
			}
		}
	}


	public static RegDocument ParseFile(string file)
	{
		var document = new RegDocument();
		document.Load(file);
		return document;
	}

	public void WriteTo(string file)
	{
		using var writer = new RegFileWriter(file);

		foreach (var keyName in Erased)
		{
			writer.DeleteKey(keyName);
		}

		foreach (var pair in this)
		{
			writer.SetKey(pair.Key);
			foreach (var item in pair.Value)
			{
				if (item.Value.IsDelete)
				{
					writer.DeleteValue(item.Key);
				}
				else
				{
					writer.SetValue(item.Key, item.Value.Value, item.Value.Kind);
				}
			}
		}
	}
}
