global using RegistryEx.Test.Properties;
global using Microsoft.Win32;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "MSTEST0006:Avoid '[ExpectedException]'")]

namespace RegistryEx.Test;

[TestClass]
public static class GlobalSetup
{
	[AssemblyInitialize]
	public static void SetupGlobal(TestContext _)
	{
		RegistryHelper.AddTokenPrivileges();
	}
}
