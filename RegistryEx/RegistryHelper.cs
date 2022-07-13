﻿using System.IO;
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
	/// 从 .NET 标准库里抄的快捷方法，增加了根键的缩写支持，为什么微软不直接提供？
	/// <br/>
	/// <see href="https://referencesource.microsoft.com/#mscorlib/microsoft/win32/registry.cs,94"/>
	/// </summary>
	/// <returns>注册表键，如果不存在则为 null</returns>
	public static RegistryKey? OpenKey(string path, bool wirte = false)
	{
		var basekeyName = path;
		var i = path.IndexOf('\\');
		if (i != -1)
		{
			basekeyName = path.Substring(0, i);
		}
		var basekey = GetBaseKey(basekeyName);

		if (i == -1 || i == path.Length)
		{
			return basekey;
		}
		else
		{
			var pathRemain = path.Substring(i + 1, path.Length - i - 1);
			return basekey?.OpenSubKey(pathRemain, wirte);
		}
	}

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
	/// 便捷的函数用于检查一个键是否存在于注册表中。
	/// </summary>
	public static bool KeyExists(string path)
	{
		var i = path.IndexOf('\\') + 1;
		if (i == 0)
		{
			return GetBaseKey(path) != null;
		}
		var basekey = GetBaseKey(path.Substring(0, i - 1));
		if (basekey == null)
		{
			return false;
		}
		path = path.Substring(i, path.Length - i);
		return basekey.ContainsSubKey(path);
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
