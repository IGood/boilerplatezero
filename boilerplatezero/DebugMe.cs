using System.Diagnostics;

namespace Bpz
{
	public static class DebugMe
	{
		public static void Go()
		{
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();
			}
			else
			{
				Debugger.Break();
			}
		}
	}
}
