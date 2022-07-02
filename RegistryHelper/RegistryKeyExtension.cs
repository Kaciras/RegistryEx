using System;
using System.IO;
using Microsoft.Win32;

namespace RegistryHelper;

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
}
