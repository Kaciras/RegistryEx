using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Benchmark")]
namespace RegistryEx.Test;

internal static class SharedTools
{
	/// <summary>
	/// 导出注册表键，相当于注册表编辑器里右键 -> 导出。
	/// </summary>
	/// <param name="file">保存的文件</param>
	/// <param name="path">注册表键</param>
	public static void Export(string file, string path)
	{
		Execute("regedit", $"/e {file} {path}");
	}

	// 必须用 regedit.exe，如果用 regedt32 可能出错，上面的一样
	public static void Import(string file)
	{
		Execute("regedit", $"/s {file}");
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

		var process = Process.Start(startInfo)!;
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
