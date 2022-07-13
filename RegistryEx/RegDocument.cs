using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		var removed = new List<string>();
		foreach (var existing in Created.Keys)
		{
			if (!existing.StartsWith(name))
			{
				continue;
			}
			if (existing.Length == name.Length ||
				existing[name.Length] == '\\')
			{
				removed.Add(existing);
			}
		}

		removed.ForEach(v => Created.Remove(v));
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

	string Normalize(string name)
	{
		return name;
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
			switch (reader.IsKey, reader.IsDelete)
			{
				case (true, false):
					key = CreateKey(reader.Key);
					break;
				case (true, true):
					DeleteKey(reader.Key);
					break;
				case (false, false):
					key[reader.Name] = new(reader.Value, reader.Kind);
					break;
				case (false, true):
					key[reader.Name] = RegistryValue.DELETED;
					break;
			}
		}
	}

	public void LoadRegistry(RegistryKey key)
	{
		foreach (var name in key.GetSubKeyNames())
		{
			using var subKey = key.OpenSubKey(name);
			LoadRegistry(subKey!);
		}

		var dict = CreateKey(key.Name);
		foreach (var name in key.GetValueNames())
		{
			dict[name] = RegistryValue.From(key, name);
		}
	}

	public void Merge(RegDocument other)
	{
		foreach (var item in other.Erased)
		{
			Erased.Add(item);
		}
		foreach (var (k, d) in other.Created)
		{
			var dict = CreateKey(k);
			foreach (var (n, v) in d) 
				dict[n] = v;
		}
	}

	public void Revert(RegDocument other)
	{
		foreach (var name in other.Erased)
		{
			using var key = RegistryHelper.OpenKey(name);
			LoadRegistry(key!);
		}
		foreach (var (name, dict) in other.Created)
		{
			using var key = RegistryHelper.OpenKey(name);
			if (key == null)
			{
				DeleteOldTree(name);
				continue;
			}

			var valueDict = CreateKey(name);
			foreach (var (vn, val) in dict)
			{
				var actual = RegistryValue.From(key, vn);
				if (!actual.Equals(val))
				{
					valueDict[vn] = actual;
				}
			}
		}
	}

	public RegDocument CreateRestorePoint()
	{
		var restoration = new RegDocument();
		restoration.Revert(this);
		return restoration;
	}

	public bool IsSuitable()
	{
		var erased = new HashSet<string>(Erased);
		foreach (var (k, d) in Created)
		{
			var key = RegistryHelper.OpenKey(k);
			if (key == null)
			{
				return false;
			}

			foreach (var (n, e) in d)
			{
				if (!e.Equals(RegistryValue.From(key, n)))
				{
					return false;
				}
			}

			if (erased.Remove(k) && key.ValueCount != d.Count)
			{
				return false;
			}
		}
		return erased
			.Select(RegistryHelper.KeyExists)
			.All(e => !e);
	}

	public void Import()
	{
		foreach (var keyName in Erased)
		{
			// delete key tree
		}
		foreach (var (k, d) in Created)
		{
			
		}
	}

	public static RegDocument ParseFile(string file)
	{
		var document = new RegDocument();
		document.Load(File.ReadAllText(file));
		return document;
	}

	public void WriteTo(Stream outputStream)
	{
		using var writer = new RegFileWriter(outputStream);

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
					writer.SetValue(name, item.Value, item.Kind);
				}
			}
		}
	}
}
