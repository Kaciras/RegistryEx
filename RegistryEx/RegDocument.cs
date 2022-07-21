using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace RegistryEx;

using ValueDict = Dictionary<string, RegistryValue>;

public class RegDocument 
{
	internal const string VER_LINE = "Windows Registry Editor Version 5.00";

	public HashSet<string> Erased { get; } = new(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, ValueDict> Created { get; } = new(StringComparer.OrdinalIgnoreCase);

	public void DeleteKey(string name)
	{
		name = Normalize(name, true);

		Erased.Add(name);

		var removed = new List<string>();
		foreach (var existing in Created.Keys)
		{
			if (!existing.StartsWith(name, StringComparison.OrdinalIgnoreCase))
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
		Erased.Add(Normalize(name, true));
	}

	public ValueDict CreateKey(string name)
	{
		name = Normalize(name, false);
		if (Created.TryGetValue(name, out var existing))
		{
			return existing;
		}
		return Created[name] = new(StringComparer.OrdinalIgnoreCase);
	}

	string Normalize(string name, bool disallowRoot)
	{
		// Key name cannot start or end with \
		if (name.Length == 0 || name[name.Length - 1] == '\\')
		{
			throw new ArgumentException($"Invalid key name: {name}");
		}

		var slash = name.IndexOf('\\');
		var i = slash;
		var root = name;

		// Key name cannot have consecutive slashs
		while (i != -1)
		{
			if (name[slash + 1] == '\\')
			{
				throw new ArgumentException($"Invalid key name: {name}");
			}
			i = name.IndexOf('\\', i + 1);
		}

		if (slash != -1)
		{
			root = name.Substring(0, slash);		
		}
		else if (disallowRoot)
		{
			throw new ArgumentException("Can not delete basekey");
		}

		// Convert basekey alias to full name
		var basekey = RegistryHelper.GetBaseKey(root);
		if (slash == -1)
		{
			return basekey.Name;
		}
		return $@"{basekey.Name}\{name.Substring(slash + 1)}";
	}

	public void LoadFile(string path)
	{
		Load(File.ReadAllText(path, Encoding.Unicode));
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

	public void Load(RegistryKey key)
	{
		foreach (var name in key.GetSubKeyNames())
		{
			using var subKey = key.OpenSubKey(name);
			Load(subKey!);
		}

		var dict = CreateKey(key.Name);
		foreach (var name in key.GetValueNames())
		{
			dict[name] = RegistryValue.From(key, name);
		}
	}

	public void Load(RegDocument other)
	{
		foreach (var item in other.Erased)
		{
			DeleteKey(item);
		}
		foreach (var (k, d) in other.Created)
		{
			CreateKey(k).AddAll(d);
		}
	}

	public void Revert(RegDocument other)
	{
		foreach (var name in other.Erased)
		{
			using var key = RegistryHelper.OpenKey(name);
			Load(key!);
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

	public bool IsSuitable
	{
		get
		{
			var erased = new HashSet<string>(Erased, Erased.Comparer);
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

				if (erased.Remove(k))
				{
					if (key.ValueCount != d.Count)
					{
						return false;
					}

					foreach (var s in key.GetSubKeyNames())
					{
						if (!Created.ContainsKey($@"{k}\{s}"))
						{
							return false;
						}
					}
				}
			}
			return !erased.Any(RegistryHelper.KeyExists);
		}
	}

	public void Import()
	{
		foreach (var keyName in Erased)
		{
			RegistryHelper.DeleteKeyTree(keyName, false);
		}
		foreach (var (k, d) in Created)
		{
			using var key = RegistryHelper.CreateKey(k);
			foreach (var (n, v) in d)
			{
				if (v.IsDelete)
				{
					key.DeleteValue(n, false);
				}
				else
				{
					key.SetValue(n, v.Value, v.Kind);
				}
			}
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
				var (value, kind) = item;
				if (item.IsDelete)
				{
					writer.DeleteValue(name);
				}
				else
				{
					writer.SetValue(name, value, kind);
				}
			}
		}
	}
}
