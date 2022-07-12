using Microsoft.Win32;

namespace RegistryEx;

public record struct RegistryValue(
	object Value,
	RegistryValueKind Kind)
{
	public bool IsDelete => Kind == RegistryValueKind.Unknown;
}
