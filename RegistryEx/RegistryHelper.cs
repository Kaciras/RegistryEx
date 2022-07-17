using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

[assembly: InternalsVisibleTo("Test")]
[assembly: InternalsVisibleTo("Benchmark")]
namespace RegistryEx;

public static class RegistryHelper
{
	/// <summary>
	/// Get the basekey by it's name, support abbreviation.
	/// </summary>
	/// <param name="name">Name or abbreviation of the basekey.</param>
	/// <returns>The key requested, or null if it doesn't exists.</returns>
	public static RegistryKey? GetBaseKey(string name) => name.ToUpper() switch
	{
		"HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
		"HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
		"HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
		"HKEY_USERS" or "HKU" => Registry.Users,
		"HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
		"HKEY_PERFORMANCE_DATA" => Registry.PerformanceData,
		_ => null, // `HKEY_DYN_DATA => Registry.DynData` is not supported.
	};

	/// <summary>
	/// Retrieves a specified key, and specifies whether write access is 
	/// to be applied to the key.
	/// </summary>
	/// <param name="path">Name or path of the key to open.</param>
	/// <param name="wirte">Set to true if you need write access to the key.</param>
	/// <returns>The key requested, or null if the operation failed.</returns>
	public static RegistryKey? OpenKey(string path, bool wirte = false)
	{
		var (basekey, subKey) = SplitForKey(path);
		if (subKey.Length == 0)
		{
			return basekey;
		}
		return basekey?.OpenSubKey(subKey, wirte);
	}

	/// <summary>
	/// Check whether the specified key exists.
	/// </summary>
	/// <param name="path">The path of the key to check.</param>
	/// <returns>true if exists, otherwise false.</returns>
	public static bool KeyExists(string path)
	{
		var (basekey, subKey) = SplitForKey(path);
		return basekey.ContainsSubKey(subKey);
	}

	/// <summary>
	/// Creates a new key or opens an existing subkey for write access.
	/// </summary>
	/// <param name="path">The path of the subkey to create or open</param>
	/// <returns>The newly created subkey, or null if the operation failed.</returns>
	public static RegistryKey CreateKey(string path)
	{
		var (basekey, subKey) = SplitForKey(path);
		return basekey.CreateSubKey(subKey);
	}

	/// <summary>
	///	Recursively deletes a key and any child subkeys.
	/// </summary>
	/// <param name="path">Key to delete</param>
	/// <param name="throwIfMissing">
	///		Indicates whether an exception should be raised if the specified subkey 
	///     cannot be found. If this argument is true and the specified subkey does not 
	///     exist, an exception is raised. If this argument is false and the 
	///     specified subkey does not exist, no action is taken.
	/// </param>
	public static void DeleteKeyTree(string path, bool throwIfMissing = true)
	{
		var (basekey, subKey) = SplitForKey(path);
		basekey.DeleteSubKeyTree(subKey, throwIfMissing);
	}

	static (RegistryKey, string) SplitForKey(string path)
	{
		var subkeyName = "";
		var root = path;

		var i = path.IndexOf('\\');
		if (i != -1)
		{
			subkeyName = path.Substring(i + 1);
			root = path.Substring(0, i);
		}

		var basekey = GetBaseKey(root);
		if (basekey == null)
		{
			throw new ArgumentException($"Invalid basekey: {root}");
		}

		return (basekey, subkeyName);
	}

	/// <summary>
	/// Set the specified value, create the key if it does not exists.
	/// </summary>
	/// <param name="path">Path of value to store data in, the last part is value name</param>
	/// <param name="value">Data to store</param>
	/// <param name="kind">Data type</param>
	public static void SetValue(string path, object value, RegistryValueKind kind)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.CreateSubKey(subkey, true);
		key.SetValue(name, value, kind);
	}

	/// <summary>
	///		Retrieves the value associated with the specified path, in the specified registry
	///     key. If the name is not found in the specified key, returns a default value that
	///     you provide, or null if the specified key does not exist.
	/// </summary>
	/// <param name="path">
	///		The full registry path of the value, beginning with a valid registry root.
	/// </param>
	/// <param name="default">
	///		The value to return if valueName does not exist.
	/// </param>
	/// <returns>
	///		null if the subkey specified by keyName does not exist; otherwise, the value
	///     associated with valueName, or defaultValue if valueName is not found.
	/// </returns>
	public static object? GetValue(string path, object? @default = null)
	{
		return GetValue(path, @default, RegistryValueOptions.None);
	}

	public static object? GetValue(string path, RegistryValueOptions options)
	{
		return GetValue(path, null, options);
	}

	public static object? GetValue(string path, object? @default, RegistryValueOptions options)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.OpenSubKey(subkey);
		return key?.GetValue(name, @default, options);
	}

	/// <summary>
	/// Retrieves the registry data type of the value associated with the specified path.
	/// </summary>
	/// <param name="path">The path of the value whose registry data type is to be retrieved.</param>
	/// <returns>The registry data type of the value associated with name.</returns>
	public static RegistryValueKind GetValueKind(string path)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.OpenSubKey(subkey);
		if (key != null)
		{
			return key.GetValueKind(name);
		}
		throw new IOException("The specified registry key doesn't exist");
	}

	/// <summary>
	/// Deletes the specified value.
	/// </summary>
	/// <param name="path">The path of the value to delete.</param>
	/// <param name="throwIfMissing">
	///		Indicates whether an exception should be raised if the specified subkey 
	///     cannot be found. If this argument is true and the specified subkey does not 
	///     exist, an exception is raised. If this argument is false and the 
	///     specified subkey does not exist, no action is taken.
	/// </param>
	public static void DeleteValue(string path, bool throwIfMissing = true)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.OpenSubKey(subkey, true);
		if (key != null)
		{
			key.DeleteValue(name, throwIfMissing);
		}
		else if (throwIfMissing)
		{
			throw new IOException("The specified registry key doesn't exist");
		}
	}

	static (RegistryKey, string, string) SplitForValue(string path)
	{
		var subkeyName = "";
		var valueName = "";
		var root = path;

		var j = path.LastIndexOf('\\');
		var i = path.IndexOf('\\');
		if (i != -1)
		{
			root = path.Substring(0, i);

			if (j == i++)
			{
				subkeyName = path.Substring(i);
			}
			else
			{
				subkeyName = path.Substring(i, j - i);
				valueName = path.Substring(j + 1);
			}
		}

		var basekey = GetBaseKey(root);
		if (basekey == null)
		{
			throw new ArgumentException($"Invalid basekey: {root}");
		}

		return (basekey, subkeyName, valueName);
	}

	/// <summary>
	/// 从注册表中读取指定 CLSID 项的默认值。
	/// </summary>
	/// <param name="clsid">CLSID值，格式{8-4-4-4-12}</param>
	/// <exception cref="DirectoryNotFoundException">如果CLSID记录不存在</exception>
	public static string GetCLSIDValue(string clsid)
	{
		using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + clsid);
		return (string?)key?.GetValue(string.Empty)
			?? throw new DirectoryNotFoundException($"CLSID {clsid} is not registred");
	}

	/// <summary>
	/// Add necesary token privilieges that some methods required.
	/// </summary>
	/// <see href="https://stackoverflow.com/a/38727406/7065321"></see>
	public static void AddTokenPrivileges()
	{
		TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");
		TokenManipulator.AddPrivilege("SeBackupPrivilege");
		TokenManipulator.AddPrivilege("SeRestorePrivilege");
	}

	/// <summary>
	/// 为当前用户设置完全控制键的权限，用户至少要有修改权限的权限。
	/// <br/>
	/// 尽管程序以管理员身份运行，仍有些注册表键没有修改权限，故需要添加一下。
	/// <code>
	/// // 使用 using 语法来自动还原：
	/// using var _ = RegistryHelper.ElevatePermission(key);
	/// </code>
	/// </summary>
	/// <param name="key">键</param>
	/// <returns>一个可销毁对象，在销毁时还原键的权限</returns>
	/// <see cref="https://stackoverflow.com/a/6491052"/>
	public static KeyElevateSession Elevate(RegistryKey baseKey, string name)
	{
		var user = WindowsIdentity.GetCurrent().User;
		var rule = new RegistryAccessRule(user, RegistryRights.FullControl, AccessControlType.Allow);
		return new KeyElevateSession(baseKey, name, rule);
	}
}
