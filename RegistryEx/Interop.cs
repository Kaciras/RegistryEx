using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RegistryEx;

/// <summary>
/// https://docs.microsoft.com/en-us/windows/win32/api/winreg
/// https://stackoverflow.com/a/17047190/7065321
/// </summary>
internal static class Interop
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct TokPriv1Luid
	{
		public int Count;
		public long Luid;
		public int Attributes;
	}

	const int SE_PRIVILEGE_DISABLED = 0x00000000;
	const int SE_PRIVILEGE_ENABLED = 0x00000002;

	const int TOKEN_QUERY = 0x00000008;
	const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

	[DllImport("advapi32.dll")]
	public static extern int RegCreateKeyTransacted(
		SafeRegistryHandle hKey,
		string lpSubKey,
		int reserved,
		string? lpClass,
		int dwOptions,
		int samDesired,
		IntPtr secAttrs,
		out SafeRegistryHandle hkResult,
		out int lpdwDisposition,
		KernelTransaction hTransaction,
		IntPtr pExtendedParemeter
	);

	[DllImport("advapi32.dll")]
	public static extern int RegOpenKeyTransacted(
		SafeRegistryHandle hKey,
		string lpSubKey,
		int ulOptions,
		int samDesired,
		out SafeRegistryHandle hkResult,
		KernelTransaction hTransaction,
		IntPtr pExtendedParemeter
	);

	[DllImport("advapi32.dll")]
	public static extern int RegDeleteKeyTransacted(
		SafeRegistryHandle hKey,
		string lpSubKey,
		int samDesired,
		int reserved,
		KernelTransaction hTransaction,
		IntPtr pExtendedParemeter
	);

	[DllImport("advapi32.dll")]
	public static extern int RegRestoreKey(SafeRegistryHandle hKey, string lpFile, uint flags);

	[DllImport("advapi32.dll")]
	public static extern int RegLoadKey(SafeRegistryHandle hKey, string lpSubKey, string lpFile);

	[DllImport("advapi32.dll")]
	public static extern int RegUnLoadKey(SafeRegistryHandle hKey, string lpSubKey);

	[DllImport("advapi32.dll")]
	public static extern int RegLoadAppKey(string file,
		out SafeRegistryHandle hkResult, RegistryRights sam, bool dwOptions, int Reserved);

	[DllImport("advapi32.dll")]
	public static extern int RegSaveKeyEx(SafeRegistryHandle hKey, string lpFile, IntPtr secAttrs, uint flags);

	[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
	static extern bool AdjustTokenPrivileges(
		IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen
	);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	static extern IntPtr GetCurrentProcess();

	[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
	static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool LookupPrivilegeValue(string? host, string name, ref long pluid);

	public static void AddPrivilege(string privilege)
	{
		Set(privilege, SE_PRIVILEGE_ENABLED);
	}

	public static void RemovePrivilege(string privilege)
	{
		Set(privilege, SE_PRIVILEGE_DISABLED);
	}

	static void Set(string privilege, int attr)
	{
		var hproc = Process.GetCurrentProcess().Handle;
		var htok = IntPtr.Zero;
		Check(OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok));

		TokPriv1Luid tp;
		tp.Count = 1;
		tp.Luid = 0;
		tp.Attributes = attr;

		Check(LookupPrivilegeValue(null, privilege, ref tp.Luid));
		Check(AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero));
	}

	public static void Check(bool @return)
	{
		if (!@return)
		{
			Check(Marshal.GetLastWin32Error());
		}
	}

	const int ERROR_SUCCESS = 0;
	const int ERROR_ACCESS_DENIED = 5;
	const int ERROR_INVALID_HANDLE = 6;
	const int ERROR_SHARING_VIOLATION = 32;
	const int ERROR_KEY_DELETED = 1018;
	const int ERROR_PRIVILEGE_NOT_HELD = 1314;
	const int ERROR_TRANSACTION_ALREADY_ABORTED = 6704;

	/// <summary>
	/// https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes
	/// https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/Interop/Windows/Interop.Errors.cs
	/// </summary>
	/// <param name="code"></param>
	public static void Check(int code)
	{
		switch (code)
		{
			case ERROR_SUCCESS:
				return;
			case ERROR_TRANSACTION_ALREADY_ABORTED:
				throw new InvalidOperationException("Transaction has already been aborted.");
			case ERROR_KEY_DELETED:
				throw new IOException("The key has been marked for deletion.");
			case ERROR_ACCESS_DENIED:
				throw new UnauthorizedAccessException();
			case ERROR_INVALID_HANDLE:
				throw new Win32Exception("Invalid handle");
			case ERROR_SHARING_VIOLATION:
				throw new IOException("The file is being used.");
			case ERROR_PRIVILEGE_NOT_HELD:
				throw new UnauthorizedAccessException("Process does not have necessary " +
					"privilege, you can use RegistryHelper.AddTokenPrivileges to add them");
			default:
				throw new Win32Exception($"Win32 API failed, code: {code}");
		}
	}
}
