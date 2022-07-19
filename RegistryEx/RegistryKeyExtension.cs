using System;
using System.IO;
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
