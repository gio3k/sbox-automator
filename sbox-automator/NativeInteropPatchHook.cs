using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;

namespace SandboxAutomator;

public class NativeInteropPatchHook
{
	private static unsafe void Initialize( Action orig )
	{
		orig();

		var n = ManagedEngine.Assemblies.EngineAssembly.Type( "NativeEngine.EngineGlobal" ).Type( "__N" );

		// EngineGlobal.__N.global_Plat_MessageBox
		delegate* unmanaged<nint, nint, void> ptr = &Native.Global.Plat_MessageBox;
		n.Field( "global_Plat_MessageBox" ).SetValue( null, (nint)ptr );

		// done for now
		Log.Info( "Patched NativeInterop" );
	}

	private Detouring.HookWrapper _hook =
		Detouring.HookWithSameName<NativeInteropPatchHook>(
			ManagedEngine.Assemblies.EngineAssembly.Type( "Managed.SandboxEngine.NativeInterop" ),
			"Initialize" );
}
