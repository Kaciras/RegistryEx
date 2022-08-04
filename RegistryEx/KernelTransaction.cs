using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RegistryEx;

public sealed class KernelTransaction : SafeHandleZeroOrMinusOneIsInvalid
{
	public KernelTransaction() : base(true)
	{
		handle = CreateTransaction(IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, null);
		Interop.Check(!IsInvalid);
	}

	protected override bool ReleaseHandle()
	{
		return CloseHandle(handle);
	}

	public void Commit()
	{
		Interop.Check(CommitTransaction(handle));
	}

	public void Rollback()
	{
		Interop.Check(RollbackTransaction(handle));
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool CloseHandle(IntPtr handle);

	[DllImport("KtmW32.dll", SetLastError = true)]
	static extern IntPtr CreateTransaction(
		IntPtr lpTransactionAttributes,
		IntPtr UOW,
		int CreateOptions,
		int IsolationLevel,
		int IsolationFlags,
		int Timeout,
		string? Description
	);

	[DllImport("KtmW32.dll", SetLastError = true)]

	static extern bool CommitTransaction(IntPtr handle);

	[DllImport("KtmW32.dll", SetLastError = true)]
	static extern bool RollbackTransaction(IntPtr handle);
}
