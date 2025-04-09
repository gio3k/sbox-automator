namespace SandboxAutomator.Core;

public interface IAutomatorPlugin
{
	public string PluginIdentifier { get; }
	public void Run();
}
