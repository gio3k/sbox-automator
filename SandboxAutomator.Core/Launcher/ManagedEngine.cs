using System.Reflection;
using SandboxAutomator.Core.Runtime;

namespace SandboxAutomator.Core.Launcher;

public static class ManagedEngine
{
	public static Dictionary<string, string> CommandLineSwitches { get; private set; }

	public struct CFileStruct( string automatorPath, string gamePath )
	{
		public readonly string GamePath = gamePath;

		public readonly string AutomatorPath = automatorPath;
		public readonly string BaseEditorLibraryPath = Path.Combine( gamePath, "addons\\tools" );
		public readonly string ManagedDllPath = Path.Combine( gamePath, "bin\\managed" );
		public readonly string NativeDllPath = Path.Combine( gamePath, "bin\\win64" );

		public string GetFullPathFromAutomatorDir( string path ) => Path.GetFullPath( path, AutomatorPath);
	}

	public static CFileStruct Files { get; private set; }

	public struct CAsmStruct( CFileStruct files )
	{
		public Assembly DevAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.GamePath, "sbox-dev.dll" ) );

		public Assembly AppSystemAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.AppSystem.dll" ) );

		public Assembly EngineAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.Engine.dll" ) );

		public Assembly ToolsAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.Tools.dll" ) );

		public Assembly SystemAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.System.dll" ) );

		public Assembly CompilingAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.Compiling.dll" ) );

		public Assembly SolutionGeneratorAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.SolutionGenerator.dll" ) );

		public Assembly ServicesAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.Services.dll" ) );

		public Assembly ReflectionAssembly { get; private set; } =
			Assembly.LoadFrom( Path.Combine( files.ManagedDllPath, "Sandbox.Reflection.dll" ) );
	}

	public static CAsmStruct Assemblies { get; private set; }

	public readonly struct CTypeStruct( CAsmStruct assemblies )
	{
		public readonly struct CSandboxTypesStruct( CAsmStruct assemblies )
		{
			public readonly Type Project = assemblies
				.EngineAssembly
				.Type( "Sandbox.Project" );

			public readonly Type LauncherEnvironment = assemblies
				.DevAssembly
				.Type( "Sandbox.LauncherEnvironment" );

			public readonly Type EditorAppSystem = assemblies
				.AppSystemAssembly
				.Type( "Sandbox.EditorAppSystem" );

			public readonly Type Api = assemblies
				.EngineAssembly
				.Type( "Sandbox.Api" );

			public readonly Type Compiler = assemblies
				.CompilingAssembly
				.Type( "Sandbox.Compiler" );
		}

		public readonly CSandboxTypesStruct Sandbox = new(assemblies);
	}

	public static CTypeStruct Types { get; private set; }

	private static void PrepareEnvironment()
	{
		Log.Info( "Preparing environment..." );

		AppDomain.CurrentDomain.AssemblyResolve +=
			( _, args ) =>
			{
				var trim = args.Name.Split( ',' )[0];
				var name = $"{Files.ManagedDllPath}\\{trim}.dll".Replace( ".resources.dll", ".dll" );
				return File.Exists( name ) ? Assembly.LoadFrom( name ) : null;
			};

		Types.Sandbox.LauncherEnvironment
			.Prop( "GamePath" )
			.SetStatic( Files.GamePath );

		Types.Sandbox.LauncherEnvironment
			.Prop( "ManagedDllPath" )
			.SetStatic( Files.ManagedDllPath );

		Types.Sandbox.LauncherEnvironment
			.Prop( "NativeDllPath" )
			.SetStatic( Files.NativeDllPath );

		Environment.CurrentDirectory = Files.GamePath;
		Environment.SetEnvironmentVariable( "FACEPUNCH_ENGINE", Files.GamePath,
			EnvironmentVariableTarget.User );

		Environment.SetEnvironmentVariable( "PATH",
			$"{Files.NativeDllPath};{Environment.GetEnvironmentVariable( "PATH" )}" );
	}

	public static void SetEnginePath( string path )
	{
		Log.Info( $"Set engine path to '{path}'" );

		Files = new CFileStruct( Environment.CurrentDirectory, path );
		Assemblies = new CAsmStruct( Files );
		Types = new CTypeStruct( Assemblies );
	}

	public static void Initialize()
	{
		Log.Info( "Initializing engine..." );

		PrepareEnvironment();

		CommandLineSwitches = Assemblies
			.SystemAssembly
			.Type( "Sandbox.Utility.CommandLine" )
			.Field( "switches" )
			.GetStatic<Dictionary<string, string>>();
	}

	public static object? StartEditor()
	{
		var editorAppSystem = Activator.CreateInstance( ManagedEngine.Types.Sandbox.EditorAppSystem );

		// Disable Steam auth
		Types.Sandbox.Api
			.Field( "UseSteamAuthentication" )
			.SetStatic( false );

		// Run AppSystem.Init
		editorAppSystem
			.T()
			.Method( "Init" )
			.InvokeFor( editorAppSystem );

		// Enable printing logs to console
		Assemblies
			.SystemAssembly
			.Type( "Sandbox.Diagnostics.Logging" )
			.Prop( "PrintToConsole" )
			.SetStatic( true );

		// We called AppSystem.Init already and AppSystem.Run will call it again
		// Make Init a dummy function
		Detouring.Hook(
			Types.Sandbox.EditorAppSystem
				.Method( "Init" ),
			( object _ ) => { }
		);

		return editorAppSystem;
	}

	public static void StartLoop( object appSystem )
	{
		// Run AppSystem.Run
		appSystem
			.T()
			.Method( "Run" )
			.InvokeFor( appSystem );
	}
}
