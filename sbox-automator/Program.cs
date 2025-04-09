global using static SandboxAutomator.Core.Logger;
using System.ComponentModel;
using System.Reflection;
using CommandLine;
using SandboxAutomator.Core;
using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;

namespace SandboxAutomator;

public class Program
{
	public class Options
	{
		[Option( 'e', "engine", HelpText = "Path to the engine root directory", Required = true )]
		public string EnginePath { get; set; } = string.Empty;

		[Option( 'g', "project", HelpText = "Path to the .sbproj", Required = true )]
		public string ProjectPath { get; set; } = string.Empty;

		[Option( 's', "script", HelpText = "Path to the automator script", Required = true )]
		public string ScriptPath { get; set; } = string.Empty;

		[Option( "generate-project", HelpText = "Generate a project for script development" )]
		public bool ShouldGenerateProject { get; set; } = false;

		[Option( 'c', "config" )] public IEnumerable<string> Config { get; set; } = [];
	}

	public static void Main( string[] args ) =>
		Parser.Default.ParseArguments<Options>( args )
			.WithParsed( Start )
			.WithNotParsed( _ => Environment.Exit( 1 ) );

	public static string ScriptPath { get; private set; } = null!;

	private static void Start( Options options )
	{
		ScriptPath = options.ScriptPath;

		if ( !File.Exists( ScriptPath ) )
		{
			Log.Info( $"Script '{ScriptPath}' not found" );
			Environment.Exit( 1 );
			return;
		}

		// Set the engine path
		ManagedEngine.SetEnginePath( options.EnginePath );

		if ( options.ShouldGenerateProject )
		{
			Log.Info( "Generating project..." );
			var path = AutomatorProjectGenerator.CreateProjectFile();
			Log.Info( $"Project generated at '{path}'" );
			Environment.Exit( 0 );
		}

		// Initialize the managed parts of the engine
		ManagedEngine.Initialize();

		_ = new EditorLibraryBuildArchiveHook();

		_ = new BuildReferencesHook();

		// Initialize the engine command line
		ManagedEngine.CommandLineSwitches["project"] = Path.GetFullPath( options.ProjectPath );

		// Launch the editor
		if ( ManagedEngine.StartEditor() is not { } appSystem )
			throw new Exception( "Failed to start editor!" );

		Task.Run( () =>
		{
			PostStart( options );
		} );

		ManagedEngine.StartLoop( appSystem );
	}

	private static void PostStart( Options options )
	{
		Log.Info( "Preparing..." );

		var scriptConfigDictionary = options.Config?
			.Select( v => v.Split( "=", 2 ) )
			.Where( v => v.Length == 2 )
			.ToDictionary( v =>
				v[0], v => v[1] );

		ManagedEngine.Assemblies.EngineAssembly
			.Type( "Sandbox.MainThread" )
			.Method( "Queue" )
			.InvokeStatic( () =>
			{
				if ( AssemblyUtils.Find( "package.toolbase" ) is not { } toolbase )
				{
					Log.Info( "Failed to find package.toolbase" );
					Environment.Exit( 2 );
					return;
				}

				Log.Info( "Running plugins" );

				foreach ( var type in toolbase.GetTypes()
					         .Where( v => v.GetInterfaces().Contains( typeof(IAutomatorPlugin) ) ) )
				{
					var instance = type.Create();

					Log.Info( $"Preparing plugin '{type.Name}'" );

					// Set plugin arguments
					if ( scriptConfigDictionary != null )
					{
						foreach ( var propertyInfo in
						         type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) )
						{
							if ( propertyInfo.GetCustomAttribute<ArgumentAttribute>() is not { } argumentAttribute )
								continue;

							if ( !scriptConfigDictionary.TryGetValue( propertyInfo.Name, out var value ) )
							{
								if ( argumentAttribute.Required )
								{
									Log.Info(
										$"Argument '{propertyInfo.Name}' required for '{type.Name}' wasn't found." );
									Environment.Exit( 1 );
									return;
								}

								continue;
							}

							var typeConverter = TypeDescriptor.GetConverter( propertyInfo.PropertyType );
							if ( typeConverter.ConvertFromString( value ) is { } converted )
							{
								propertyInfo.SetValue( instance, converted );
								continue;
							}

							Log.Info( $"Failed to turn value for argument '{propertyInfo.Name}' into a {type.Name}." );
							Environment.Exit( 1 );
						}
					}

					type.Method( "Run" ).InvokeFor( instance );
				}
			} );
	}
}
