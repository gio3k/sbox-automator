using System.Collections;
using System.Reflection;
using System.Threading;
using Editor.Wizards;
using SandboxAutomator.Core;
using SandboxAutomator.Core.Runtime;

public class NothingPlugin : IAutomatorPlugin
{
	public string PluginIdentifier => "nothing";

	public void Run()
	{
		Environment.Exit( 0 );
	}
}
