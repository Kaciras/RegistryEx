using System;
using System.Runtime.InteropServices;

namespace RegistryEx;

/// <summary>
/// 
/// <see href="https://stackoverflow.com/a/17047190/7065321"></see>
/// </summary>
static class TokenManipulator
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
		var hproc = GetCurrentProcess();
		var htok = IntPtr.Zero;
		Interop.Check(OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok));

		TokPriv1Luid tp;
		tp.Count = 1;
		tp.Luid = 0;
		tp.Attributes = attr;

		Interop.Check(LookupPrivilegeValue(null, privilege, ref tp.Luid));
		Interop.Check(AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero));
	}
}
