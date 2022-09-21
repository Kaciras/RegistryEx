using System.Security.Principal;
using System.Linq;
using System.Security.AccessControl;

namespace RegistryEx.Test;

internal readonly struct TestFixture : IDisposable
{
	public static TestFixture Import(string file) => new TestFixture(file);

	private TestFixture(string file)
	{
		SharedTools.Import(@$"Resources\{file}.reg");
	}

	public void Dispose()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}

	public static void RemoveCurrentUserACL(RegistryKey key)
	{
		var user = WindowsIdentity.GetCurrent().User!;
		var accessControl = key.GetAccessControl();

		var rule = new RegistryAccessRule(user, RegistryRights.FullControl, AccessControlType.Deny);
		accessControl.SetAccessRule(rule);

		accessControl.SetAccessRuleProtection(true, true);
		key.SetAccessControl(accessControl);
	}
}
