using Microsoft.Win32;

namespace RegistryEx;

public record struct PlainRegistryValue(
	object Value,
	RegistryValueKind Kind)
{
	public bool IsDelete => Kind == RegistryValueKind.Unknown;
}
