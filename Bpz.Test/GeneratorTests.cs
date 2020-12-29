using Bpz.Wpf;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bpz.Test
{
	public class GeneratorTests
	{
		[TestCase("MinimalDP1.cs")]
		[TestCase("MinimalDP2.cs")]
		[TestCase("MinimalDP3.cs")]
		[TestCase("MinimalDP4.cs")]
		[TestCase("MinimalDP5.cs")]
		[TestCase("MinimalDP6.cs")]
		[TestCase("MinimalDP7.cs")]
		[TestCase("AttachedDP1.cs")]
		[TestCase("AttachedDP2.cs")]
		[TestCase("PropertyChangedHandlers.cs")]
		[TestCase("Coercion.cs")]
		public void GenStuff(string resourceName)
		{
			using var source = Resources.GetEmbeddedResource(resourceName);
			var sourceText = SourceText.From(source);
			GeneratorHost.RunGenerator(sourceText, new DependencyPropertyGenerator());
		}
	}

	internal static class Resources
	{
		public static Stream GetEmbeddedResource(string name)
		{
			var assembly = Assembly.GetExecutingAssembly();
			string? resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));
			Assert.IsNotNull(resourceName);
			return assembly.GetManifestResourceStream(resourceName!)!;
		}
	}
}
