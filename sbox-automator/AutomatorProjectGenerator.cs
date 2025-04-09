using System.Text;
using System.Web;
using SandboxAutomator.Core.Launcher;

namespace SandboxAutomator;

public class AutomatorProjectGenerator
{
	private static string Tab( int depth ) => new('\t', depth);

	private static string Normalize( string path ) => HttpUtility.HtmlEncode( path );

	public static string GenerateCsprojContents()
	{
		var builder = new StringBuilder();
		builder.AppendLine( Tab( 0 ) + "<Project Sdk=\"Microsoft.NET.Sdk\">" );
		{
			builder.AppendLine( Tab( 1 ) + "<PropertyGroup>" );
			builder.AppendLine( Tab( 2 ) + "<AssemblyName>Automator Workspace</AssemblyName>" );
			builder.AppendLine( Tab( 2 ) + "<PackageId>Automator Workspace</PackageId>" );
			builder.AppendLine( Tab( 2 ) + "<EnableDefaultCompileItems>false</EnableDefaultCompileItems>" );
			builder.AppendLine( Tab( 1 ) + "</PropertyGroup>" );

			builder.AppendLine( Tab( 1 ) + $"<Import Project=\"Base Editor Library.csproj\"/>" );

			// Script file(s)
			builder.AppendLine( Tab( 1 ) + "<ItemGroup>" );
			{
				builder.AppendLine( Tab( 2 ) + $"<Compile Include=\"{Normalize( Program.ScriptPath )}\"/>" );
			}
			builder.AppendLine( Tab( 1 ) + "</ItemGroup>" );


			/*foreach ( var sourceGroup in Program.PluginManager.PluginSourceGroups )
			{
				builder.AppendLine( Tab( 1 ) + "<ItemGroup>" );

				foreach ( var sourceFile in sourceGroup.Files )
				{
					builder.AppendLine( Tab( 2 ) +
					                    $"<Compile Include=\"{Normalize( sourceFile.FullPath )}\">" );
					builder.AppendLine( Tab( 3 ) +
					                    $"<Link>Automator Plugins\\{Normalize( sourceGroup.PluginDirectoryName )}\\{Normalize( sourceFile.RelativePath )}</Link>" );
					builder.AppendLine( Tab( 2 ) + "</Compile>" );
				}

				builder.AppendLine( Tab( 1 ) + "</ItemGroup>" );
			}

			builder.AppendLine( Tab( 1 ) + "<ItemGroup>" );
			{
				builder.AppendLine( Tab( 2 ) +
				                    $"<Compile Include=\"{Normalize( libraryDirectory )}\\**\\*.cs\">" );
				builder.AppendLine( Tab( 3 ) +
				                    "<Link>Automator Libraries\\%(RecursiveDir)%(Filename)%(Extension)</Link>" );
				builder.AppendLine( Tab( 2 ) + "</Compile>" );
			}
			builder.AppendLine( Tab( 1 ) + "</ItemGroup>" );*/

			builder.AppendLine( Tab( 1 ) + "<ItemGroup>" );
			{
				foreach ( var i in (List<string>) ["**\\*.cs", "**\\*.cs.scss", "**\\*.razor.cs", "**\\*.razor.scss"] )
				{
					builder.AppendLine( Tab( 2 ) +
					                    $"<Compile Include=\"{i}\" Exclude=\"obj\\**\\*.*\">" );
					builder.AppendLine( Tab( 3 ) +
					                    "<Link>Base Editor Library\\%(RecursiveDir)%(Filename)%(Extension)</Link>" );
					builder.AppendLine( Tab( 2 ) + "</Compile>" );
				}
			}
			builder.AppendLine( Tab( 1 ) + "</ItemGroup>" );

			// Add library assemblies
			builder.AppendLine( Tab( 1 ) + "<ItemGroup>" );
			{
				var pathToSandboxAutomatorCoreAssembly = typeof(ManagedEngine).Assembly.Location;
				builder.AppendLine( Tab( 2 ) + $"<Reference Include=\"{pathToSandboxAutomatorCoreAssembly}\" />" );
			}
			builder.AppendLine( Tab( 1 ) + "</ItemGroup>" );
		}
		builder.AppendLine( Tab( 0 ) + "</Project>" );

		return builder.ToString();
	}

	public static string CreateProjectFile()
	{
		var csprojPath = Path.Combine( ManagedEngine.Files.BaseEditorLibraryPath,
			"code\\Sandbox Automator.csproj" );
		File.WriteAllText( csprojPath, GenerateCsprojContents() );
		return csprojPath;
	}
}
