using System;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace RegistryEx;

public static class RegistryKeyExtension
{
	/// <summary>
	/// Deletes the key and any child subkeys recursively. and specifies whether an exception
	/// is raised if the key is not found.
	/// </summary>
	/// <param name="throwOnMissing">
	/// Indicates whether an exception should be raised if the specified subkey cannot be found.
	/// </param>
	/// <exception cref="IOException">If the key doesn't exist and throwOnMissing is true</exception>
	public static void Delete(this RegistryKey key, bool throwOnMissing = true)
	{
		try
		{
			key.DeleteSubKeyTree(string.Empty, throwOnMissing);
		}
		catch (ArgumentException)
		{
			throw new IOException($"{key.Name} doesn't exist");
		}
	}

	/// <summary>
	/// Gets a value indicating whether a register key exists. 
	/// </summary>
	/// <returns>true if the key exists, otherwise false</returns>
	public static bool Exists(this RegistryKey key)
	{
		try
		{
			_ = key.SubKeyCount;
			return true;
		}
		catch (IOException e) when (e.HResult == 1018)
		{
			return false; // ERROR_FILE_NOT_FOUND 
		}
	}

	public static bool ContainsSubKey(this RegistryKey key, string name)
	{
		using var subKey = key.OpenSubKey(name);
		return subKey != null;
	}

	public static RegistryValueKind GetValueKind(
		this RegistryKey key,
		string path,
		string name)
	{
		var subKey = key.OpenSubKey(path);
		if (subKey != null)
		{
			return subKey.GetValueKind(name);
		}
		throw new IOException("The specified registry key doesn't exist");
	}

	//[return: NotNullIfNotNull("defaultValue")]
	public static T? GetValue<T>(
		this RegistryKey key,
		string path,
		string name,
		T? defaultValue = default)
	{
		using var subKey = key.OpenSubKey(path);
		if (subKey == null)
		{
			return defaultValue;
		}
		return (T?)subKey.GetValue(name, defaultValue);
	}

	public static void DeleteValue(
		this RegistryKey key,
		string path,
		string name)
	{
		using var subKey = key.OpenSubKey(path, true);
		subKey?.DeleteValue(name, false);
	}

	public static void SetValue(
		this RegistryKey key,
		string path,
		string name,
		object value)
	{
		using var subKey = key.CreateSubKey(path, true);
		subKey.SetValue(name, value);
	}

	/// <summary>
	///		Copies the specified registry key, along with its values, access rules, and subkeys, 
	///		to the specified destination key.
	/// </summary>
	/// <param name="subkey">
	///		The name of the key. 
	///		This key must be a subkey of the key identified by the key parameter.
	///		This parameter can also be NULL.
	/// </param>
	/// <param name="dest">
	///		The destination key. The calling process must have KEY_CREATE_SUB_KEY access to the key.
	///	</param>
	public static void CopyTree(this RegistryKey key, string? subkey, RegistryKey dest)
	{
		using var transaction = new KernelTransaction();
		CopyTree(key, subkey, dest, transaction);
		transaction.Commit();
	}

	static void CopyTree(RegistryKey src, string? subkey, RegistryKey dest, KernelTransaction tx)
	{
		if (subkey != null)
		{
			using var srcSubkey = src.OpenSubKey(subkey)!;
			CopyTree(srcSubkey, null, dest, tx);
			dest.SetAccessControl(srcSubkey.GetAccessControl());
		}
		else
		{
			foreach (var name in src.GetSubKeyNames())
			{
				using var target = dest.CreateSubKey(name, tx);
				CopyTree(src, name, target, tx);
			}
			foreach (var name in src.GetValueNames())
			{
				dest.SetValue(name, src.GetValue(name)!, src.GetValueKind(name));
			}
		}
	}

	/// <summary>
	///		Creates the specified registry key and associates it with a transaction. 
	///		If the key already exists, the function opens it. 
	/// </summary>
	/// <param name="subkey">
	///		The name of a subkey that this function opens or creates. 
	///		The subkey specified must be a subkey of the key identified by the key parameter.
	/// </param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <returns>The newly created subkey, or null if the operation failed.</returns>
	public static RegistryKey CreateSubKey(
		this RegistryKey key,
		string subkey,
		KernelTransaction transaction)
	{
		const RegistryRights WRITEABLE = RegistryRights.WriteKey | RegistryRights.ReadKey;
		return CreateSubKey(key, subkey, transaction, WRITEABLE);
	}

	/// <summary>
	///		Creates the specified registry key and associates it with a transaction. 
	///		If the key already exists, the function opens it. 
	/// </summary>
	/// <param name="subkey">
	///		The name of a subkey that this function opens or creates. 
	///		The subkey specified must be a subkey of the key identified by the key parameter.
	/// </param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <param name="rights">
	///		A mask that specifies the access rights for the key to be created.
	///	</param>
	/// <returns>The newly created subkey, or null if the operation failed.</returns>
	public static RegistryKey CreateSubKey(
		this RegistryKey key, 
		string subkey,
		KernelTransaction transaction,
		RegistryRights rights)
	{
		var hRessult = Interop.RegCreateKeyTransacted(
				key.Handle,
				subkey,
				0,
				null,
				0,
				(int)rights,
				IntPtr.Zero,
				out var hkResult,
				out _,
				transaction,
				IntPtr.Zero);

		Interop.Check(hRessult);
		return RegistryKey.FromHandle(hkResult);
	}

	/// <summary>
	///		Opens the specified registry key and associates it with a transaction.
	/// </summary>
	/// <param name="subkey">
	///		The name of the registry subkey to be opened.
	/// </param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <param name="writeable">
	///		true to indicate the opened subkey is writable; otherwise, false.
	/// </param>
	public static RegistryKey? OpenSubKey(
		this RegistryKey key,
		string subkey,
		KernelTransaction transaction,
		bool writeable = false)
	{
		var rights = writeable
			? RegistryRights.WriteKey | RegistryRights.ReadKey
			: RegistryRights.ReadKey;

		return OpenSubKey(key, subkey, transaction, rights);
	}

	/// <summary>
	///		Opens the specified registry key and associates it with a transaction.
	/// </summary>
	/// <param name="subkey">
	///		The name of the registry subkey to be opened.
	/// </param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <param name="rights">
	///		A mask that specifies the desired access rights to the key.
	/// </param>
	public static RegistryKey? OpenSubKey(
		this RegistryKey key,
		string subkey,
		KernelTransaction transaction,
		RegistryRights rights)
	{
		var hRessult = Interop.RegOpenKeyTransacted(
				key.Handle,
				subkey,
				0,
				(int)rights,
				out var hkResult,
				transaction,
				IntPtr.Zero);

		if (hRessult == 2)
		{
			return null;
		}
		Interop.Check(hRessult);
		return RegistryKey.FromHandle(hkResult);
	}

	/// <summary>
	///		Deletes a subkey and its values from the specified platform-specific view 
	///		of the registry as a transacted operation.
	///	<br/>
	///		The subkey to be deleted must not have subkeys. 
	///		To delete a key and all its subkeys, you need to enumerate the subkeys and delete them individually.
	/// </summary>
	/// <param name="subkey">
	///		The name of the key to be deleted. 
	///		This key must be a subkey of the key specified by the value of the key parameter.
	///	</param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <param name="throwOnMissing">
	///		Indicates whether an exception should be raised if the specified subkey cannot
	///		be found. If this argument is true and the specified subkey does not exist, an
	///		exception is raised. If this argument is false and the specified subkey does
	///		not exist, no action is taken.
	/// </param>
	public static void DeleteSubKey(
		this RegistryKey key,
		string subkey,
		KernelTransaction transaction,
		bool throwOnMissing = true)
	{
		var hResult = Interop.RegDeleteKeyTransacted(key.Handle, subkey, 0, 0, transaction, IntPtr.Zero);

		if (hResult == 5 && key.SubKeyCount > 0)
		{
			throw new InvalidOperationException("The subkey to be deleted must not have subkeys.");
		}
		else if (hResult != 2) // ERROR_FILE_NOT_FOUND
		{
			Interop.Check(hResult);
		}
		else if (throwOnMissing)
		{
			throw new ArgumentException("The specified subkey is not exists.");
		}
	}

	/// <summary>
	///		Recursively deletes a subkey and any child subkeys as a transacted operation.
	/// </summary>
	/// <param name="subkey">
	///		SubKey to delete.
	/// </param>
	/// <param name="transaction">
	///		An active transaction.
	/// </param>
	/// <param name="throwOnMissing">
	///		Indicates whether an exception should be raised if the specified subkey cannot
	///		be found.
	///	</param>
	public static void DeleteSubKeyTree(
		this RegistryKey key,
		string subkey,
		KernelTransaction transaction,
		bool throwOnMissing = true)
	{
		using var opened = key.OpenSubKey(subkey, transaction, true);
		if (opened != null)
		{
			foreach (var name in opened.GetSubKeyNames())
			{
				opened.DeleteSubKeyTree(name, transaction, true);
			}

			key.DeleteSubKey(subkey, transaction, true);
		}
		else if (throwOnMissing)
		{
			throw new ArgumentException("The specified subkey is not exists.");
		}
	}

	/// <summary>
	///		Saves the specified key and all of its subkeys and values to a registry file.
	/// </summary>
	/// <param name="file">
	///		The name of the file in which the specified key and subkeys are to be saved.
	/// </param>
	public static void SaveHive(this RegistryKey key, string file)
	{
		File.Delete(file);
		Interop.Check(Interop.RegSaveKeyEx(key.Handle, file, IntPtr.Zero, 2));
	}

	/// <summary>
	///		Creates a subkey under HKEY_USERS or HKEY_LOCAL_MACHINE and loads the data 
	///		from the specified registry hive into that subkey.
	///		This function always loads information at the top of the registry hierarchy.
	/// </summary>
	/// <param name="subKey">
	///		The name of the key to be created the key. This subkey is where the 
	///		registration information from the file will be loaded.
	/// </param>
	/// <param name="file">
	///		The name of the file containing the registry data. This file must be a local 
	///		file that was created with the RegSaveKey function. 
	///		If this file does not exist, a file is created with the specified name.
	/// </param>
	public static void LoadHive(this RegistryKey key, string subKey, string file)
	{
		Interop.Check(Interop.RegLoadKey(key.Handle, subKey, file));
	}

	/// <summary>
	///		Unloads the specified registry key and its subkeys from the registry.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="subKey">The name of the subkey to be unloaded.</param>
	public static void UnLoadHive(this RegistryKey key, string subKey)
	{
		Interop.Check(Interop.RegUnLoadKey(key.Handle, subKey));
	}

	/// <summary>
	///		Reads the registry information in a specified file and copies it over the specified key. 
	///		This registry information may be in the form of a key and multiple levels of subkeys.
	/// </summary>
	/// <param name="file">
	///		The name of the file with the registry information.
	/// </param>
	public static void RestoreHive(this RegistryKey key, string file)
	{
		Interop.Check(Interop.RegRestoreKey(key.Handle, file, 0));
	}
}
