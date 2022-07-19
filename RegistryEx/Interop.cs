using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RegistryEx;

/// <summary>
/// https://docs.microsoft.com/en-us/windows/win32/api/winreg/
/// </summary>
internal static class Interop
{
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

	public static void Check(bool @return)
	{
		if (!@return)
		{
			Check(Marshal.GetLastWin32Error());
		}
	}

	/// <summary>
	/// https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes
	/// https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/Interop/Windows/Interop.Errors.cs
	/// </summary>
	/// <param name="code"></param>
	public static void Check(int code)
	{
		switch (code)
		{
			case 0:
				return; // ERROR_SUCCESS
			case 5:
				// ERROR_ACCESS_DENIED 
				throw new UnauthorizedAccessException();
			case 6:
				// ERROR_INVALID_HANDLE 
				throw new Win32Exception("Invalid handle");
			case 32:
				// ERROR_SHARING_VIOLATION
				throw new IOException("The file is being used.");
			case 1314:
				// ERROR_PRIVILEGE_NOT_HELD 
				throw new UnauthorizedAccessException("RegistryHelper.AddTokenPrivileges");
			default:
				throw new Win32Exception($"Win32 API failed, code: {code}");
		}
	}
}
