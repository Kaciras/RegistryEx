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
	/// 从 .NET 标准库里抄的快捷方法，增加了根键的缩写支持，为什么微软不直接提供？
	/// <br/>
	/// <see href="https://referencesource.microsoft.com/#mscorlib/microsoft/win32/registry.cs,94"/>
	/// </summary>
	/// <returns>注册表键，如果不存在则为 null</returns>
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
	/// 便捷的函数用于检查一个键是否存在于注册表中。
	/// </summary>
	public static bool KeyExists(string path)
	{
		var (basekey, subKey) = SplitForKey(path);
		return basekey.ContainsSubKey(subKey);
	}

	public static RegistryKey CreateKey(string path)
	{
		var (basekey, subKey) = SplitForKey(path);
		return basekey.CreateSubKey(subKey);
	}

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

	public static void SetValue(string path, object value, RegistryValueKind kind)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.CreateSubKey(subkey, true);
		key.SetValue(name, value, kind);
	}

	public static object? GetValue(string path, object? @default)
	{
		var (basekey, subkey, name) = SplitForValue(path);
		using var key = basekey.OpenSubKey(subkey);
		return key != null ? key.GetValue(name, @default) : default;
	}

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

		var key = GetBaseKey(root);
		if (key == null)
		{
			throw new ArgumentException($"Invalid basekey: {root}");
		}

		return (key, subkeyName, valueName);
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
