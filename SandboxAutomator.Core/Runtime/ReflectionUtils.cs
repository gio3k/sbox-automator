#nullable disable
using System.Reflection;
using System.Runtime.Loader;

namespace SandboxAutomator.Core.Runtime;

public struct ReflectionObject( object value )
{
	public readonly object Value = value;
	public readonly Type Type = value?.GetType();
}

public static class AssemblyUtils
{
	// Find a loaded assembly
	public static Assembly Find( string name )
	{
		return AssemblyLoadContext.All.SelectMany( v => v.Assemblies )
			.FirstOrDefault( v => v.GetName().Name == name );
	}
}

public static class ReflectionUtils
{
	public static Type T( this object o ) => o.GetType();

	public static Type Type( this Assembly assembly, string typeName ) => assembly.GetType( typeName );

	public static Type Type( this Type type, string nestedTypeName ) => type.GetNestedType( nestedTypeName,
		BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

	public static MethodInfo Method( this Type type, string methodName, Type[] overloads = null )
	{
		if ( type == null )
			throw new Exception( $"Method('{methodName}'): type null" );
		const BindingFlags bindingFlags =
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var method = overloads == null
			? type.GetMethod( methodName, bindingFlags )
			: type.GetMethod( methodName, bindingFlags, overloads );
		return method;
	}

	public static MethodInfo MethodRecursive( this Type type, string methodName, Type[] overloads = null )
	{
		if ( type == null )
			throw new Exception( $"Method('{methodName}'): type null" );

		var current = type;
		while ( current != null )
		{
			var method = current.Method( methodName, overloads );
			if ( method != null )
				return method;

			current = current.BaseType;
		}

		return null;
	}

	public static ConstructorInfo Ctor( this Type type, Type[] overloads = null )
	{
		if ( type == null )
			throw new Exception( "Ctor(): type null" );
		const BindingFlags bindingFlags =
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var method = overloads == null
			? type.GetConstructor( bindingFlags, [] )
			: type.GetConstructor( bindingFlags, overloads );
		return method;
	}

	public static PropertyInfo Prop( this Type type, string propertyName )
	{
		if ( type == null )
			throw new Exception( $"Prop('{propertyName}'): type null" );
		const BindingFlags bindingFlags =
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
			BindingFlags.FlattenHierarchy;
		return type.GetProperty( propertyName, bindingFlags );
	}

	public static PropertyInfo PropRecursive( this Type type, string propertyName )
	{
		var current = type;
		while ( current != null )
		{
			var x = current.Prop( propertyName );
			if ( x != null )
				return x;

			current = current.BaseType;
		}

		return null;
	}

	public static FieldInfo Field( this Type type, string fieldName )
	{
		if ( type == null )
			throw new Exception( $"Field('{fieldName}'): type null" );
		const BindingFlags bindingFlags =
			BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		return type.GetField( fieldName, bindingFlags );
	}

	public static FieldInfo FieldRecursive( this Type type, string fieldName )
	{
		var current = type;
		while ( current != null )
		{
			var x = current.Field( fieldName );
			if ( x != null )
				return x;

			current = current.BaseType;
		}

		return null;
	}

	public static T Create<T>( this Type type )
	{
		if ( type == null )
			throw new Exception( $"Create(): type null" );
		if ( !type.IsSubclassOf( typeof(T) ) )
			throw new Exception( $"Create(): type not subclass of {typeof(T).FullName})" );
		return (T)Activator.CreateInstance( type );
	}

	public static object Create( this Type type ) => Create<object>( type );

	public static T InvokeFor<T>( this MethodInfo methodInfo, object self, params object[] args )
	{
		if ( methodInfo == null )
			throw new Exception( $"InvokeFor(): method null" );
		if ( self == null )
			throw new Exception( $"InvokeFor(): instance / self null" );
		return (T)methodInfo.Invoke( self, args );
	}

	public static object InvokeFor( this MethodInfo methodInfo, object self, params object[] args ) =>
		methodInfo.InvokeFor<object>( self, args );

	public static T InvokeStatic<T>( this MethodInfo methodInfo, params object[] args )
	{
		if ( methodInfo == null )
			throw new Exception( $"InvokeStatic(): method null" );
		return (T)methodInfo.Invoke( null, args );
	}

	public static object InvokeStatic( this MethodInfo methodInfo, params object[] args ) =>
		methodInfo.InvokeStatic<object>( args );

	private static T GetFor<T>( this PropertyInfo propertyInfo, object self ) => (T)propertyInfo.GetValue( self );
	public static T GetStatic<T>( this PropertyInfo propertyInfo ) => (T)propertyInfo.GetValue( null );

	private static void SetFor( this PropertyInfo propertyInfo, object self, object value ) =>
		propertyInfo.SetValue( self, value );

	public static void SetStatic( this PropertyInfo propertyInfo, object value ) =>
		propertyInfo.SetValue( null, value );

	private static T GetFor<T>( this FieldInfo fieldInfo, object self ) => (T)fieldInfo.GetValue( self );
	public static T GetStatic<T>( this FieldInfo fieldInfo ) => (T)fieldInfo.GetValue( null );

	private static void SetFor( this FieldInfo fieldInfo, object self, object value ) =>
		fieldInfo.SetValue( self, value );

	public static void SetStatic( this FieldInfo fieldInfo, object value ) => fieldInfo.SetValue( null, value );

	// Instance
	public static ReflectionObject? ToReflectionObject( this object self ) =>
		self != null ? new ReflectionObject( self ) : null;

	// Get Instance Prop as T
	public static T Prop<T>( this ReflectionObject self, string name ) =>
		self.Type.PropRecursive( name ).GetFor<T>( self.Value );

	// Get Instance Field as T
	public static T Field<T>( this ReflectionObject self, string name )
	{
		if ( self.Value == null )
			throw new Exception( $"Field('{name}'): instance / self is null" );
		return self.Type.FieldRecursive( name ).GetFor<T>( self.Value );
	}

	// Get Instance Prop as object
	public static object Prop( this ReflectionObject self, string name ) =>
		self.Prop<object>( name );

	// Get Instance Field as object
	public static object Field( this ReflectionObject self, string name ) =>
		self.Field<object>( name );

	public static void Prop( this ReflectionObject self, string name, object value ) =>
		self.Type.PropRecursive( name ).SetFor( self.Value, value );

	public static void Field( this ReflectionObject self, string name, object value ) =>
		self.Type.FieldRecursive( name ).SetFor( self.Value, value );

	public static object InitField( this ReflectionObject self, string name )
	{
		var x = self.Type.FieldRecursive( name );
		var instance = x.FieldType.Create();
		x.SetFor( self.Value, instance );
		return instance;
	}

	public static object InitProp( this ReflectionObject self, string name )
	{
		var x = self.Type.PropRecursive( name );
		var instance = x.PropertyType.Create();
		x.SetFor( self.Value, instance );
		return instance;
	}

	public static T Invoke<T>( this ReflectionObject self, string name, params object[] args ) =>
		self.Type.MethodRecursive( name ).InvokeFor<T>( self.Value, args );

	public static object Invoke( this ReflectionObject self, string name, params object[] args ) =>
		self.Invoke<object>( name, args );
}
