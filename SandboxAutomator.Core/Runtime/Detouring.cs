using System.Reflection;
using System.Runtime.Loader;
using SandboxAutomator.Core.Launcher;

namespace SandboxAutomator.Core.Runtime;

public static class Detouring
{
	public static readonly Type HookType;

	static Detouring()
	{
		var loadContext = AssemblyLoadContext.Default;
		HookType =
			loadContext
				.LoadFromAssemblyPath( Path.Combine( ManagedEngine.Files.ManagedDllPath, "MonoMod.RuntimeDetour.dll" ) )
				.Type( "MonoMod.RuntimeDetour.Hook" );
	}

	public readonly struct HookWrapper( object instance ) : IDisposable
	{
		private readonly IDisposable _instance = instance as IDisposable;

		public void Dispose()
		{
			Log.Info( "Hook disposed" );
			_instance?.Dispose();
		}
	}

	public static HookWrapper HookWithSameName<TRedirectTo>( Type originalType, string name )
	{
		return Hook(
			originalType.Method( name ),
			typeof(TRedirectTo).Method( name )
		);
	}

	public static HookWrapper Hook( MethodBase from, MethodInfo to )
	{
		Log.Info( $"HookUtils.Hook [from = {from}, to = {to}]" );
		return new HookWrapper( Activator.CreateInstance( HookType, from, to )! );
	}

	public static HookWrapper Hook( MethodBase from, Action to )
	{
		Log.Info( $"HookUtils.Hook [from = {from}, to = {to}]" );
		return new HookWrapper( Activator.CreateInstance( HookType, from, to )! );
	}

	public static HookWrapper Hook<T>( MethodBase from, T to )
	{
		Log.Info( $"HookUtils.Hook [from = {from}, to = {to}]" );
		return new HookWrapper( Activator.CreateInstance( HookType, from, to )! );
	}
}
