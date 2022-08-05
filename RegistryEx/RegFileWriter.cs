using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace RegistryEx;

/// <summary>
/// Writer for Registration Entries (.reg) file.
/// </summary>
/// <see href="https://docs.microsoft.com/en-us/windows/win32/sysinfo/registry-value-types"/>
public readonly struct RegFileWriter : IDisposable
{
	private static readonly char[] Escape = { '"', '\\' };

	readonly StreamWriter writer;

	public RegFileWriter(Stream stream)
	{
		writer = new(stream, Encoding.Unicode);
		writer.NewLine = "\r\n";
		writer.WriteLine(RegDocument.VER_LINE);
	}

	public void Dispose()
	{
		writer.Dispose();
	}

	public void DeleteKey(string path)
	{
		writer.WriteLine();
		writer.Write("[-");
		writer.Write(path);
		writer.WriteLine(']');
	}

	public void SetKey(string path)
	{
		writer.WriteLine();
		writer.Write('[');
		writer.Write(path);
		writer.WriteLine(']');
	}

	public void DeleteValue(string name)
	{
		WriteName(name);
		writer.WriteLine("=-");
	}

	public void SetValue(string name, IEnumerable<string> value)
	{
		SetValue(name, value, RegistryValueKind.MultiString);
	}

	public void SetValue(string name, byte[] value)
	{
		SetValue(name, value, RegistryValueKind.Binary);
	}

	public void SetValue(string name, int value)
	{
		SetValue(name, value, RegistryValueKind.DWord);
	}

	public void SetValue(string name, long value)
	{
		SetValue(name, value, RegistryValueKind.QWord);
	}

	public void SetValue(string name, string value)
	{
		SetValue(name, value, RegistryValueKind.String);
	}

	public void SetValue(string name, object value, RegistryValueKind kind)
	{
		WriteName(name);
		writer.Write('=');

		switch (kind)
		{
			case RegistryValueKind.DWord:
				writer.Write("dword:");
				writer.WriteLine(Convert.ToInt32(value).ToString("x8"));
				break;
			case RegistryValueKind.String:
				WriteString((string)value);
				writer.WriteLine();
				break;
			case RegistryValueKind.Binary:
				writer.Write("hex:");
				WriteBinaryLine((byte[])value);
				break;
			case RegistryValueKind.None:
				writer.Write("hex(0):");
				WriteBinaryLine((byte[])value);
				break;
			case RegistryValueKind.ExpandString:
				writer.Write("hex(2):");
				WriteBinaryLine((string)value);
				break;
			case RegistryValueKind.MultiString:
				writer.Write("hex(7):");
				WriteBinaryLine((IEnumerable<string>)value);
				break;
			case RegistryValueKind.QWord:
				writer.Write("hex(b):");
				WriteBinaryLine(Convert.ToInt64(value));
				break;
			default:
				throw new ArgumentException("Invalid kind: " + kind);
		}
	}

	void WriteName(string name)
	{
		if (name == "")
		{
			writer.Write('@');
		}
		else
		{
			WriteString(name);
		}
	}

	void WriteString(string value)
	{
		var i = 0;
		var k = value.IndexOfAny(Escape);

		writer.Write('"');
		while (k != -1)
		{
			writer.Write(value.Substring(i, k - i));
			writer.Write('\\');
			i = k;
			k = value.IndexOfAny(Escape, i + 1);
		}
		writer.Write(value.Substring(i));
		writer.Write('"');
	}

	void WriteBinaryLine(IEnumerable<string> value)
	{
		var sb = new StringBuilder();
		foreach (var item in value)
		{
			if (item.Length == 0)
			{
				continue;
			}
			sb.Append(item).Append('\0');
		}
		WriteBinaryLine(sb.Append('\0').ToString());
	}

	void WriteBinaryLine(string value)
	{
		WriteBinaryLine(Encoding.Unicode.GetBytes(value));
	}

	void WriteBinaryLine(long value)
	{
		WriteBinaryLine(BitConverter.GetBytes(value));
	}

	// Regedit wrap lines at 80, but I haven't implemented that.
	void WriteBinaryLine(byte[] bytes)
	{
		if (bytes.Length == 0)
		{
			writer.WriteLine();
			return;
		}

		var i = 0;
		for (; i < bytes.Length - 1; i++)
		{
			writer.Write(bytes[i].ToString("x2"));
			writer.Write(',');
		}
		writer.WriteLine(bytes[i].ToString("x2"));
	}
}
