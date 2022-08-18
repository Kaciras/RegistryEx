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

	public ICollection<string> Erased { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public Dictionary<string, ValueDict> Created { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Add a delete key entry, and remove create key entries
	/// which are subkey of the deleted.
	/// </summary>
	/// <param name="name">The name of the key to delete</param>
	public void DeleteKey(string name)
	{
		Erased.Add(name);
		Created.Keys
			.Where(e => RegistryHelper.IsSubKey(name, e))
			.ToList().ForEach(v => Created.Remove(v));
	}

	/// <summary>
	/// Get the value dictionary of the create key entry, or add one if it not exists.
	/// </summary>
	/// <param name="name">The key name</param>
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
		static bool SubNotSame(string anc, string des)
		{
			return anc.Length != des.Length && RegistryHelper.IsSubKey(anc, des);
		}

		Erased
			.Where(des => Erased.Any(anc => SubNotSame(anc, des)))
			.ToList().ForEach(v => Erased.Remove(v));

		Created
			.Where(pair => pair.Value.Count == 0)
			.Select(pair => pair.Key)
			.Where(anc => Created.Keys.Any(des => SubNotSame(anc, des)))
			.ToList().ForEach(v => Created.Remove(v));
	}

	/// <summary>
	/// Load entries from a .reg file.
	/// </summary>
	/// <param name="path">The file path</param>
	public void LoadFile(string path)
	{
		Load(File.ReadAllText(path, Encoding.Unicode));
	}

	/// <summary>
	/// Load entries from .reg file content.
	/// </summary>
	/// <param name="content">The content of the .reg file</param>
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

	/// <summary>
	/// Load entries from Registry, like export in regedit.exe.
	/// </summary>
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
	/// Merge a RegDocument object to this. If entries have same name,
	/// the entry in the other will overwrite the one in this.
	/// <code>
	/// RegDocument a = RegDocument.ParseFile(...);
	/// RegDocument b = RegDocument.ParseFile(...);
	/// 
	/// // merge then execute
	/// a.Load(b);
	/// a.Execute();
	/// 
	/// // is equivalent to
	/// a.Execute();
	/// b.Execute();
	/// </code>
	/// </summary>
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
	/// Add entries reverse to the other, these entries will restore keys and 
	/// values affected by the other to their current state in Registry.

	/// </summary>
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
	/// call Execute() has no effect.
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
	/// Execute directives in the document, equivalent to command `regedit /s`.
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

	/// <summary>
	/// Serialize the document in .reg file format.
	/// </summary>
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
