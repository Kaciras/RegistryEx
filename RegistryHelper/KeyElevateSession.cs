using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace RegistryHelper;

public sealed class KeyElevateSession : IDisposable
{
	readonly RegistryKey key;
	readonly RegistryAccessRule rule;

	readonly IdentityReference oldOwner;
	readonly RegistryAccessRule oldRule;

	internal KeyElevateSession(RegistryKey baseKey, string name, RegistryAccessRule rule)
	{
		var key = baseKey.OpenSubKey(
			name,
			RegistryKeyPermissionCheck.ReadWriteSubTree,
			RegistryRights.TakeOwnership);

		if (key == null)
		{
			throw new IOException("Registry key not exists", 1018);
		}

		this.key = key;
		this.rule = rule;

		var security = key.GetAccessControl();
		var identity = rule.IdentityReference;

		oldOwner = security.GetOwner(typeof(SecurityIdentifier))!;
		security.SetOwner(identity);
		key.SetAccessControl(security);

		oldRule = security.GetAccessRules(true, false, identity.GetType())
			.Cast<RegistryAccessRule>()
			.FirstOrDefault(r => r.IdentityReference.Equals(identity));

		security.SetAccessRule(rule);
		key.SetAccessControl(security);
	}

	public void Dispose()
	{
		using (key)
		{
			try
			{
				var security = key.GetAccessControl();
				if (oldRule != null)
				{
					security.SetAccessRule(oldRule);
				}
				else
				{
					security.RemoveAccessRule(rule);
				}

				security.SetOwner(oldOwner);
				key.SetAccessControl(security);
			}
			catch (IOException e) when (e.HResult == 1018)
			{
				// key is deleted, nothing to do.
			}
		}
	}
}
