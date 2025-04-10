using System.Runtime.InteropServices;

namespace SandboxAutomator.Native;

public static class Global
{
	[UnmanagedCallersOnly]
	public static void Plat_MessageBox( nint title, nint message )
	{
		var titleM = Marshal.PtrToStringAnsi( title );
		var messageM = Marshal.PtrToStringAnsi( message );

		Console.WriteLine( $"Alert ({titleM}): {messageM}" );

		Environment.Exit( 1 );
	}
}
