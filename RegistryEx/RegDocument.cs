using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;

namespace RegistryEx;

using ValueDict = Dictionary<string, RegistryValue>;

public class RegDocument
{
	internal const string VER_LINE = "Windows Registry Editor Version 5.00";

	public ICollection<string> Erased { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, ValueDict> Created { get; } = new(StringComparer.OrdinalIgnoreCase);

	public void DeleteKey(string name)
	{
		Erased.Add(name);

		var removed = new List<string>();
		foreach (var existing in Created.Keys)
		{
			if (RegistryHelper.IsSubKey(name, existing))
			{
				removed.Add(existing);
			}
		}

		removed.ForEach(v => Created.Remove(v));
	}

	public ValueDict CreateKey(string name)
	{
		if (Created.TryGetValue(name, out var existing))
		{
			return existing;
		}
		return Created[name] = new(StringComparer.OrdinalIgnoreCase);
	}


	/// <summary>
	/// Remove redundant key entries that not affect execution result.
	/// <br/>
	/// For example, the second line create it preceding keys, so the first line is redundant.
	/// <code>
	/// [HKEY_CURRENT_USER\_RH_Test_]
	/// [HKEY_CURRENT_USER\_RH_Test_\Sub]
	/// </code>
	/// For performance reason, RegDocument does not remove them automatically.
	/// </summary>
	public void Compact()
	{
		var removed = new List<string>();
		foreach (var des in Erased)
		{
			foreach (var anc in Erased)
			{
				if (RegistryHelper.IsSubKey(anc, des) && !ReferenceEquals(anc,des))
				{
					removed.Add(des);
					break;
				}
			}
		}
		removed.ForEach(v => Erased.Remove(v));

		removed.Clear();
		foreach (var (anc, dict) in Created)
		{
			if (dict.Count > 0)
			{
				continue;
			}
			foreach (var des in Created.Keys)
			{
				
				if (RegistryHelper.IsSubKey(anc, des) && !ReferenceEquals(anc, des))
				{
					removed.Add(anc);
					break;
				}
			}
		}
		removed.ForEach(v => Created.Remove(v));
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

	/// <summary>
	/// Merge a RegDocument object to this. after the call this document will 
	/// </summary>
	/// <param name="other"></param>
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

	/// <summary>
	/// Add directives reverse to the other, these directives will restore keys and 
	/// values affected by the other to their current state.
	/// </summary>
	/// <param name="other"></param>
	public void Revert(RegDocument other)
	{
		foreach (var name in other.Erased)
		{
			using var key = RegistryHelper.OpenKey(name);
			if (key != null)
			{
				Load(key);
			}
		}
		foreach (var (name, dict) in other.Created)
		{
			using var key = RegistryHelper.OpenKey(name);
			if (key == null)
			{
				Erased.Add(name);
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

	/// <summary>
	/// Create a new RegDocument that have directives to revert this document changes
	/// to current state.
	/// </summary>
	/// <returns>The new dorument</returns>
	public RegDocument CreateRestorePoint()
	{
		var restoration = new RegDocument();
		restoration.Revert(this);
		return restoration;
	}

	/// <summary>
	/// If is true, the information stored in the document is already in Registry,
	/// </summary>
	public bool IsSuitable
	{
		get
		{
			var erased = new HashSet<string>(Erased, StringComparer.OrdinalIgnoreCase);
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

	/// <summary>
	/// Execute directives in the document, this method is equivalent to command `regedit /s`.
	/// </summary>
	public void Execute()
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
		document.LoadFile(file);
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
