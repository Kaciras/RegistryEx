using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace RegistryEx;

using ValueDict = Dictionary<string, RegistryValue>;

public class RegDocument 
{
	internal const string VER_LINE = "Windows Registry Editor Version 5.00";

	public HashSet<string> Erased { get; } = new();

	public Dictionary<string, ValueDict> Created { get; } = new();

	public void DeleteKey(string name)
	{
		var s = name.IndexOf('\\') + 1;
		if (s == 0 || s == name.Length)
		{
			throw new ArgumentException("Can not delete root key");
		}

		Erased.Add(name);

		foreach (var existing in Created.Keys)
		{
			if (!existing.StartsWith(name))
			{
				continue;
			}
			if (existing.Length == name.Length ||
				existing[name.Length] == '\\')
			{
				Created.Remove(existing);
			}
		}
	}

	public void DeleteOldTree(string name)
	{
		Erased.Add(name);
	}

	public ValueDict CreateKey(string name)
	{
		if (Created.TryGetValue(name, out var e))
		{
			return e;
		}
		return Created[name] = new ValueDict();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="content"></param>
	public void Load(string content)
	{
		var reader = new RegFileReader(content);
		ValueDict key = null!; // The reader has ensured key exists.

		while (reader.Read())
		{
			if (reader.IsKey)
			{
				if (reader.IsDelete)
				{
					DeleteKey(reader.Key);
				}
				else
				{
					key = CreateKey(reader.Key);
				}
			}
			else
			{
				key[reader.Name] = reader.IsDelete 
					? default
					: new RegistryValue(reader.Value, reader.Kind);
			}
		}
	}

	public void LoadRegistry(RegistryKey key)
	{
		var dict = CreateKey(key.Name);
		foreach (var name in key.GetValueNames())
		{
			var kind = key.GetValueKind(name);
			var value = key.GetValue(name)!;
			dict[name] = new RegistryValue(value, kind);
		}

		foreach (var name in key.GetSubKeyNames())
		{
			using var subKey = key.OpenSubKey(name);
			LoadRegistry(subKey!);
		}
	}

	public RegDocument CreateRestorePoint()
	{
		var restoration = new RegDocument();
		foreach (var name in Erased)
		{
			RegistryHelper.OpenKey(name);
		}
		throw new InvalidOperationException();
	}

	public static RegDocument ParseFile(string file)
	{
		var document = new RegDocument();
		document.Load(File.ReadAllText(file));
		return document;
	}

	public void WriteTo(string file)
	{
		using var writer = new RegFileWriter(file);

		foreach (var keyName in Erased)
		{
			writer.DeleteKey(keyName);
		}

		foreach (var (key, values) in Created)
		{
			writer.SetKey(key);
			foreach (var (name, item) in values)
			{
				if (item.IsDelete)
				{
					writer.DeleteValue(name);
				}
				else
				{
					writer.SetValue(key, item.Value, item.Kind);
				}
			}
		}
	}
}
