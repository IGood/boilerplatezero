// Copyright © Ian Good

using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Bpz.CodeAnalysis
{
	public static class AnalyzerConfigOps
	{
		/// <summary>
		/// Get a MSBuild property value for the given key.
		/// </summary>
		public static bool TryGetBuildProperty(this AnalyzerConfigOptionsProvider options, string key, [NotNullWhen(true)] out string? value)
		{
			return options.GlobalOptions.TryGetValue("build_property." + key, out value);
		}
	}
}
