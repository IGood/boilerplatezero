// Copyright © Ian Good

using System.Diagnostics;

namespace Bpz
{
	public static class DebugMe
	{
		public static void Go()
		{
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			else
			{
				Debugger.Launch();
			}
		}
	}
}
