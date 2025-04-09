using System.Collections;
using System.Runtime.CompilerServices;
using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;
using SandboxAutomator.Utils;

namespace SandboxAutomator;

public class EditorLibraryBuildArchiveHook
{
	[MethodImpl( MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization )]
	public static void BuildArchive( Action<object, object> orig, object self, object output )
	{
		orig( self, output );

		var compilerName = self.ToReflectionObject()?
			.Prop<string>( "Name" );

		if ( compilerName != "toolbase" )
			return;

		if ( output.ToReflectionObject()?
			    .Prop( "Archive" )
			    .ToReflectionObject()?
			    .Prop( "SyntaxTrees" ) is not IList syntaxTrees )
		{
			Log.Warn( "Failed to get CodeArchive.SyntaxTrees!" );
			return;
		}

		if ( self.ToReflectionObject()?
			    .Field( "config" )
			    .ToReflectionObject()?.Invoke( "GetParseOptions" ) is not { } config )
		{
			Log.Warn( "Failed to get Compiler.config.GetParseOptions!" );
			return;
		}

		var fileInfo = new FileInfo( Program.ScriptPath );
		var contents = File.ReadAllText( fileInfo.FullName );
		syntaxTrees.Add( SyntaxTreeReflection.Parse( contents, fileInfo.Name, config ) );
	}

	private Detouring.HookWrapper _hook =
		Detouring.HookWithSameName<EditorLibraryBuildArchiveHook>( ManagedEngine.Types.Sandbox.Compiler,
			"BuildArchive" );
}
