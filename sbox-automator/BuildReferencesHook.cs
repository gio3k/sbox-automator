using System.Runtime.CompilerServices;
using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;

namespace SandboxAutomator;

public class BuildReferencesHook
{
	[MethodImpl( MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization )]
	public static object BuildReferences( Func<object, object, object> orig, object self, object archive )
	{
		var references = archive.ToReflectionObject()?
			.Prop<HashSet<string>>( "References" );

		if ( !references!.Contains( "SandboxAutomator.Core" ) ) references.Add( "SandboxAutomator.Core" );

		return orig( self, archive );
	}

	private Detouring.HookWrapper _hook =
		Detouring.HookWithSameName<BuildReferencesHook>( ManagedEngine.Types.Sandbox.Compiler,
			"BuildReferences" );
}
