using System.Diagnostics;

namespace SandboxAutomator.Core;

public static class Logger
{
	public static class Log
	{
		public static bool DebugEnabled = false;

		private const string DebugPrefix = "[DBG]";
		private const string InfoPrefix = "[INF]";
		private const string WarnPrefix = "[WRN]";
		private const string ErrorPrefix = "[ERR]";
		private const string FatalPrefix = "[FTL]";

		public static void Debug( object v )
		{
			if ( DebugEnabled )
				Console.WriteLine( $"{DebugPrefix} {v}" );
		}

		public static void Info( object v )
		{
			Console.ResetColor();
			Console.WriteLine( $"{InfoPrefix} ({PreviousMethod()}) {v}" );
		}

		public static void Warn( object v ) => Console.WriteLine( $"{WarnPrefix} ({PreviousMethod()}) {v}" );
		public static void Error( object v ) => Console.WriteLine( $"{ErrorPrefix} ({PreviousMethod()}) {v}" );
		public static void Fatal( object v ) => Console.WriteLine( $"{FatalPrefix} ({PreviousMethod()}) {v}" );

		private static string PreviousMethod()
		{
			var st = new StackTrace();
			var sf = st.GetFrame( 2 );
			return sf.GetMethod().Name;
		}
	}
}
