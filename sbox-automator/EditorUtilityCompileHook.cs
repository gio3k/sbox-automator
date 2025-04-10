using System.Collections;
using System.Runtime.CompilerServices;
using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;
using SandboxAutomator.Utils;

namespace SandboxAutomator;

public class EditorUtilityCompileHook
{
	private static object Compile( Func<object, object, object> orig, object project, object logOutput )
	{
		return Task.Run( async () =>
		{
			try
			{
				var task = (Task)orig( project, logOutput );
				await task.WaitAsync( new CancellationToken() );
				return task.ToReflectionObject()?.Prop( "Result" )!;
			}
			catch ( Exception e )
			{
				Log.Error( $"Detected compile failure in Editor context: {e.Message}" );
				Log.Info( "Stopping" );
				Environment.Exit( 1 );
			}

			return null;
		} );
	}

	private Detouring.HookWrapper _hook =
		Detouring.HookWithSameName<EditorUtilityCompileHook>(
			ManagedEngine.Assemblies.ToolsAssembly.Type( "Editor.EditorUtility" ).Type( "Projects" ),
			"Compile" );
}
