using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace RegistryEx;

public class RegFileWriter : IDisposable
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
		writer.WriteLine();
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
		SetValue(name, string.Empty, RegistryValueKind.None);
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

	public void SetValue(string name, object value)
	{
		SetValue(name, value, RegistryValueKind.String);
	}

	public void SetValue(string name, object value, RegistryValueKind kind)
	{
		if (name == string.Empty)
		{
			writer.Write('@');
		}
		else
		{
			WriteString(name);
		}

		writer.Write('=');

		switch (kind)
		{
			case RegistryValueKind.None:
				writer.WriteLine('-');
				break;
			case RegistryValueKind.DWord:
				writer.Write("dword:");
				writer.WriteLine(((int)value).ToString("x8"));
				break;
			case RegistryValueKind.String:
				WriteString(name);
				writer.WriteLine();
				break;
			case RegistryValueKind.ExpandString:
				writer.Write("hex(2):");
				WriteBinaryLine((string)value);
				break;
			case RegistryValueKind.Binary:
				writer.Write("hex:");
				WriteBinaryLine((byte[])value);
				break;
			case RegistryValueKind.MultiString:
				writer.Write("hex(7):");
				WriteBinaryLine((string)value);
				break;
			case RegistryValueKind.QWord:
				writer.Write("hex(b):");
				WriteBinaryLine((long)value);
				break;
			default:
				throw new ArgumentException("Invalid kind: " + kind);
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
