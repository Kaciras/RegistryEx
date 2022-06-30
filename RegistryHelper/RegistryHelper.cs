using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

[assembly: InternalsVisibleTo("Test")]
namespace RegistryHelper;

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
		_ => null, // 微软的 API 在不存在时返回 null，这里也保持一致而不是用异常。
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

	public static bool IsTODO(string file)
	{
		var reader = new RegFileReader(File.ReadAllText(file, Encoding.Unicode));
		var expected = true;

		while (expected && reader.Read())
		{
			if (reader.IsKey)
			{
				var exists = KeyExists(reader.Key);
				expected = reader.IsDelete ^ exists;
			}
			else if (reader.IsDelete)
			{
				expected = Registry.GetValue(reader.Key, reader.Name, null) == null;
			}
			else
			{
				expected = CheckValueInDB(reader.Key,
					reader.Name, reader.Value, reader.Kind);
			}
		}

		return !expected;
	}

	/// <summary>
	/// 检查 Reg 文件里的一个值是否已经存在于注册表中。
	/// </summary>
	/// <param name="key">键路径</param>
	/// <param name="name">值名</param>
	/// <param name="valueStr">Reg文件里字符串形式的值</param>
	/// <param name="kind">值类型</param>
	static bool CheckValueInDB(string key, string name, object expected, RegistryValueKind kind)
	{
		using var keyObj = OpenKey(key);
		var actual = keyObj.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);

		// Binary 和 MultiString 返回的是数组，需要用 SequenceEqual 对比。
		bool ConvertAndCheck<T>()
		{
			if (actual is not T[] || actual == null)
			{
				return false;
			}
			return ((T[])expected).SequenceEqual((T[])actual);
		}

		return kind switch
		{
			RegistryValueKind.MultiString => ConvertAndCheck<string>(),
			RegistryValueKind.Binary => ConvertAndCheck<byte>(),
			RegistryValueKind.Unknown or RegistryValueKind.None => throw new Exception("Invalid kind"),
			_ => expected.Equals(actual),
		};
	}

	/// <summary>
	/// 导出注册表键，相当于注册表编辑器里右键 -> 导出。
	/// </summary>
	/// <param name="file">保存的文件</param>
	/// <param name="path">注册表键</param>
	public static void Export(string file, string path) => Execute("regedit", $"/e {file} {path}");

	// 必须用 regedit.exe，如果用 regedt32 可能出错，上面的一样
	public static void Import(string file) => Execute("regedit", $"/s {file}");

	/// <summary>
	/// 执行命令并等待完成，检查退出码，已经设置来重定向了输入和禁止显示窗口。
	/// <br/>
	/// 如果命令以非零值退出，则会抛出异常，异常信息使用 stderr 或 stdout，请保证命令的输出不要太长。
	/// </summary>
	/// <param name="file">文件名</param>
	/// <param name="args">参数</param>
	/// <returns>进程对象</returns>
	/// <exception cref="SystemException">如果命令执行失败</exception>
	static Process Execute(string file, string args = "")
	{
		var startInfo = new ProcessStartInfo(file)
		{
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		var process = Process.Start(startInfo);

		process.WaitForExit();
		if (process.ExitCode == 0)
		{
			return process;
		}

		var message = process.StandardError.ReadToEnd();
		if (string.IsNullOrEmpty(message))
		{
			message = process.StandardOutput.ReadToEnd();
		}
		throw new SystemException($"Command failed({process.ExitCode})：{message}");
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

	public static void AddTokenPrivileges()
	{
		TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");
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
