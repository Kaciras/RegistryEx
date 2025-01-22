using System;

namespace RegistryEx;

/// <summary>
/// Indicates the changes that should be reported from RegNotifyChangeKeyValue.
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regnotifychangekeyvalue"/>
[Flags]
public enum RegNotifyFilter : uint
{
	/// <summary>
	/// Notify the caller if a subkey is added or deleted.
	/// </summary>
	NAME = 0x00000001,

	/// <summary>
	/// Notify the caller of changes to the attributes of the key,
	/// such as the security descriptor information. 
	/// </summary>
	ATTRIBUTES = 0x00000002,

	/// <summary>
	/// Notify the caller of changes to a value of the key. 
	/// This can include adding or deleting a value, or changing an existing value.
	/// </summary>
	LAST_SET = 0x00000004,

	/// <summary>
	/// Notify the caller of changes to the security descriptor of the key.
	/// </summary>
	SECURITY = 0x00000008,
}
