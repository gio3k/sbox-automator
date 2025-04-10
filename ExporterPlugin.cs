using System.Collections;
using System.Reflection;
using System.Threading;
using Editor.Wizards;
using SandboxAutomator.Core;
using SandboxAutomator.Core.Runtime;

public class ExporterPlugin : IAutomatorPlugin
{
	public string PluginIdentifier => "exporter";

	[Argument( Required = true )] public string OutputDirectory { get; set; }
	[Argument( Required = true )] public uint AppId { get; set; }

	public void Run()
	{
		typeof(StandaloneWizard).StartRecordingInstances();

		Wizard.OpenWindow<StandaloneWizard>( Project.Current, 500, 500 );

		var standaloneWizard = (StandaloneWizard)typeof(StandaloneWizard).GetLastInstance();
		DoStandaloneWizard( standaloneWizard );
	}

	private async Task<bool> NextPageAsync( StandaloneWizard standaloneWizard )
	{
		await Task.Delay( 100 );

		var steps = standaloneWizard.ToReflectionObject()?.Field<IList>( "Steps" );

		var currentPage = standaloneWizard.ToReflectionObject()?
			.Prop( "Current" );

		Log.Info( $"NextPageAsync... (current page = {currentPage})" );

		if ( currentPage == null )
			return false;

		Log.Info( "NextPageAsync... Waiting until we can proceed..." );
		for ( int i = 0; i < 7; i++ ) // wait at least 500*7 ms
		{
			var canProceed = currentPage.ToReflectionObject()?
				.Invoke<bool>( "CanProceed" );

			if ( canProceed == true )
				break;

			await Task.Delay( 500 );
		}

		Log.Info( "NextPageAsync... Updating project..." );
		await EditorUtility.Projects.Updated( Project.Current );

		if ( steps != null && steps.Count != 0 && steps[^1] == currentPage )
		{
			var p = standaloneWizard.Parent;
			while ( p.IsValid() )
			{
				Log.Info( "NextPageAsync... hit last page, closing" );

				if (p is BaseWindow)
				{
					p.Close();
					return true;
				}

				p = p.Parent;
			}
		}

		Log.Info( "NextPageAsync... Moving to next step..." );
		{
			var i = steps.IndexOf( currentPage );
			var next = steps[i + 1];

			if ( await currentPage.ToReflectionObject()?
				    .Invoke<Task<bool>>( "FinishAsync" )! == false )
				return false;

			foreach ( var step in steps )
			{
				step.ToReflectionObject()?.Prop( "Visible", false );
			}

			await standaloneWizard.ToReflectionObject()?
				.Field( "_current" )?
				.ToReflectionObject()?
				.Field<CancellationTokenSource>( "TokenSource" )?.CancelAsync()!;

			standaloneWizard.ToReflectionObject()?
				.Field( "_current", next );
		}

		await SwitchCurrentPage( standaloneWizard );

		await Task.Delay( 100 );

		standaloneWizard.ToReflectionObject()?
			.Invoke( "Update" );

   		await Task.Delay( 100 );

		return true;
	}

	private async Task SwitchCurrentPage( StandaloneWizard standaloneWizard )
	{
		try
		{
			if ( standaloneWizard.ToReflectionObject() is not { } wizard )
				return;

			wizard.Field( "loading", true );

			if ( wizard.Field( "_current" ).ToReflectionObject() is not { } current )
				return;

			current.Field( "TokenSource", new CancellationTokenSource() );

			await current.Invoke<Task>( "OpenAsync" );

			wizard.Field( "loading", false );
		}
		catch ( Exception e )
		{
			Log.Error( e );
		}
	}

	internal async void DoStandaloneWizard( StandaloneWizard standaloneWizard )
	{
		// Prepare export config	
		var config = standaloneWizard.ToReflectionObject()?
			.Field<ExportConfig>( "Config" );

		config!.TargetDir = OutputDirectory;
		config.AppId = AppId;

		// Go through the wizard
		await NextPageAsync( standaloneWizard );

		if ( standaloneWizard.ToReflectionObject()?
			    .Field<IList>( "Steps" )[1].ToReflectionObject() is not { } currentPage )
			throw new Exception( "currentPage is null" );

		if ( !currentPage.Field<bool>( "CompileSuccessful" ) )
		{
			Log.Info( "Compile failed!" );
			Environment.Exit( 1 );
		}

		await NextPageAsync( standaloneWizard );

		await NextPageAsync( standaloneWizard );

		await NextPageAsync( standaloneWizard );

		Environment.Exit( 0 );
	}
}
