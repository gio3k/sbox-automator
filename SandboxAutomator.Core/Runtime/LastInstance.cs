namespace SandboxAutomator.Core.Runtime;

public static class LastInstance
{
	private static Dictionary<Type, object> Instances { get; } = new();

	public static void ConstructorReplacement( Action<object> orig, object self )
	{
		orig( self );

		var type = self.GetType();
		Instances.Add( type, self );
		Log.Info( $"Set LastInstance for '{type.Name}'" );
	}

	public static void Hook( Type type ) =>
		Detouring.Hook( type.Ctor(), typeof(LastInstance).Method( "ConstructorReplacement" ) );

	public static void StartRecordingInstances( this Type type ) => Hook( type );

	public static object? Get( Type type ) => Instances.GetValueOrDefault( type );
	public static object? GetLastInstance( this Type type ) => Instances.GetValueOrDefault( type );
}
