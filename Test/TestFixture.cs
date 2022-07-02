namespace RegistryEx.Test;

internal readonly struct TestFixture : IDisposable
{
	public static TestFixture Import(string file) => new TestFixture(file);

	private TestFixture(string file)
	{
		RegistryHelper.Import(@$"Resources\{file}.reg");
	}

	public void Dispose()
	{
		Registry.CurrentUser.DeleteSubKeyTree("_RH_Test_", false);
	}
}
