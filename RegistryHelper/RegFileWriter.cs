using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace RegistryHelper;

public class RegFileWriter : IDisposable
{
	public int BinaryWrapLength { get; set; } = 21;

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
		if (name == string.Empty)
		{
			writer.WriteLine("@=\"\"");
		}
		else
		{
			WriteValueName(name);
			writer.WriteLine('-');
		}
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
			writer.Write("@=");
		} 
		else
		{
			WriteValueName(name);
		}

		switch (kind)
		{
			case RegistryValueKind.String:
				writer.Write('"');
				writer.Write(value);
				writer.Write('"');
				break;
			case RegistryValueKind.ExpandString:
				writer.Write("hex(2):");
				WriteBinary((string)value);
				break;
			case RegistryValueKind.Binary:
				writer.Write("hex:");
				WriteBinary((byte[])value);
				break;
			case RegistryValueKind.DWord:
				writer.Write("dword:");
				writer.Write(((int)value).ToString("X8"));
				break;
			case RegistryValueKind.MultiString:
				writer.Write("hex(7):");
				WriteBinary((string)value);
				break;
			case RegistryValueKind.QWord:
				writer.Write("hex(b):");
				WriteBinary((long)value);
				break;
			default:
				throw new ArgumentException("Invalid kind: " + kind);
		}

		writer.WriteLine();
	}

	void WriteValueName(string name)
	{
		writer.Write('"');
		writer.Write(name);
		writer.Write('"');
		writer.Write('=');
	}

	void WriteBinary(string value)
	{
		WriteBinary(Encoding.Unicode.GetBytes(value));
	}

	void WriteBinary(long value)
	{
		WriteBinary(BitConverter.GetBytes(value));
	}

	void WriteBinary(byte[] bytes)
	{
		var wrapIndex = BinaryWrapLength;

		for (int i = 0; i < bytes.Length; i++)
		{
			writer.Write(bytes[i].ToString("X2"));

			if (i != bytes.Length - 1)
			{
				writer.Write(',');
			}

			if (i == wrapIndex)
			{
				writer.WriteLine('\\');
				wrapIndex += BinaryWrapLength;
			}
		}
	}
}
