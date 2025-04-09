using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using SandboxAutomator.Core.Runtime;

namespace SandboxAutomator.Utils;

public class SyntaxTreeReflection
{
	public static object Parse( string text, string path, object parseOptions )
	{
		var assembly = AssemblyLoadContext.Default.Assemblies.SingleOrDefault(
			v => v.GetName().Name == "Microsoft.CodeAnalysis.CSharp" );

		var type = assembly.Type( "Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree" );
		var method = type.GetMethod( "ParseText", BindingFlags.Public | BindingFlags.Static, null,
			[
				typeof(string), assembly.Type( "Microsoft.CodeAnalysis.CSharp.CSharpParseOptions" ), typeof(string),
				typeof(Encoding), typeof(CancellationToken)
			],
			null );

		return method!.Invoke( null, [text, parseOptions, path, Encoding.UTF8, default(CancellationToken)] )!;
	}
}
