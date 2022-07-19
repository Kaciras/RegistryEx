global using RegistryEx.Test.Properties;
global using Microsoft.Win32;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegistryEx.Test;

[TestClass]
public class GlobalSetup
{
	[AssemblyInitialize]
	public static void SetupGlobal(TestContext _)
	{
		RegistryHelper.AddTokenPrivileges();
	}
}
