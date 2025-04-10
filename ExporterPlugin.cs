using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using Editor.Wizards;
using SandboxAutomator.Core;
using SandboxAutomator.Core.Launcher;
using SandboxAutomator.Core.Runtime;
using FileSystem = Editor.FileSystem;

public class ExporterPlugin : IAutomatorPlugin
{
	public string PluginIdentifier => "exporter";

	[Argument( Required = true )] public string OutputDirectory { get; set; }
	[Argument( Required = true )] public uint AppId { get; set; }

	private object _wizard;

	public void Run()
	{
		EditorEvent.Register( this );

		typeof(StandaloneWizard).StartRecordingInstances();

		Wizard.OpenWindow<StandaloneWizard>( Project.Current, 500, 500 );

		var standaloneWizard = (StandaloneWizard)typeof(StandaloneWizard).GetLastInstance();

		// Prepare export config	
		var config = standaloneWizard.ToReflectionObject()?
			.Field<ExportConfig>( "Config" );

		config!.TargetDir = ManagedEngine.Files.GetFullPathFromAutomatorDir( OutputDirectory );
		config.AppId = AppId;

		DoStandaloneWizard( standaloneWizard );
	}

	private RealTimeSince _timeout;

	[EditorEvent.Frame]
	private void Tick()
	{
		if ( _wizard is not Widget widget || !widget.IsValid() )
		{
			_wizard = null;
			HandleCompletion();
			return;
		}

		//if ( _timeout < 1 )
		//	return;

		_timeout = 0;

		if ( _wizard.ToReflectionObject() is not { } wizard )
		{
			//Log.Info( "1" );
			return;
		}

		var loading = wizard.Field<bool>( "loading" );
		if ( loading )
		{
			//Log.Info( "2" );
			return;
		}

		if ( wizard.Field<object>( "_current" ).ToReflectionObject() is not { } current )
		{
			//Log.Info( "3" );
			return;
		}

		if ( !current.Invoke<bool>( "CanProceed" ) )
		{
			//Log.Info( "4" );
			return;
		}

		Log.Info( $"!!! proceeding (current = {current.Type})" );

		SetConfig( (StandaloneWizard)wizard.Value );

		wizard.Invoke( "NextPage" );
	}

	private static bool AllPages_IsAutoStep( object self ) => false;

	internal void SetConfig( StandaloneWizard standaloneWizard )
	{
		// Prepare export config	
		var config = standaloneWizard.ToReflectionObject()?
			.Field<ExportConfig>( "Config" );
		config!.TargetDir =
			ManagedEngine.Files.GetFullPathFromAutomatorDir( OutputDirectory );
		config.AppId = AppId;

		foreach ( var step in standaloneWizard.ToReflectionObject()?.Field<IList>( "Steps" ) )
		{
			step.ToReflectionObject()?.Prop( "PublishConfig", config );
		}
	}

	internal void DoStandaloneWizard( StandaloneWizard standaloneWizard )
	{
		SetConfig( standaloneWizard );

		// Patch IsAutoStep
		foreach ( var type in typeof(BaseWizardPage).Assembly.GetTypes()
			         .Where( v => v.IsSubclassOf( typeof(BaseWizardPage) ) && v != typeof(BaseWizardPage) ) )
		{
			Detouring.Hook( type.Prop( "IsAutoStep" ).GetMethod!,
				typeof(ExporterPlugin).Method( "AllPages_IsAutoStep" ) );
		}

		_timeout = 0;
		_wizard = standaloneWizard;
	}

	internal void HandleCompletion()
	{
		Environment.Exit( 0 );
	}
}
