using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RegistryEx;

internal static class InternalUtils
{
	/// <summary>
	/// Add missing deconstruct support for KeyValuePair.
	/// This method is implemented in .NET Standard 2.1.
	/// </summary>
	public static void Deconstruct<T1, T2>(
		this KeyValuePair<T1, T2> tuple,
		out T1 key, 
		out T2 value)
    {
        key = tuple.Key;
        value = tuple.Value;
    }

	/// <summary>
	/// 执行命令并等待完成，检查退出码，已经设置来重定向了输入和禁止显示窗口。
	/// <br/>
	/// 如果命令以非零值退出，则会抛出异常，异常信息使用 stderr 或 stdout。
	/// </summary>
	/// <param name="file">文件名</param>
	/// <param name="args">参数</param>
	/// <returns>进程对象</returns>
	/// <exception cref="SystemException">如果命令执行失败</exception>
	public static Process Execute(string file, string args = "")
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
}
