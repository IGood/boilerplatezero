// Copyright © Ian Good

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
		// These aren't really tests, but we do get to set breakpoints & step through our source generator with these.
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
		[TestCase("FxPropMetadata.cs")]
		public void GenProps(string resourceName)
		{
			using var source = Resources.GetEmbeddedResource(resourceName);
			var sourceText = SourceText.From(source);
			GeneratorHost.RunGenerator(sourceText, new DependencyPropertyGenerator());
		}

		// These aren't really tests, but we do get to set breakpoints & step through our source generator with these.
		[TestCase("RoutedEvent1.cs")]
		[TestCase("RoutedEvent2.cs")]
		[TestCase("RoutedEvent3.cs")]
		[TestCase("AttachedEvent1.cs")]
		public void GenEvents(string resourceName)
		{
			using var source = Resources.GetEmbeddedResource(resourceName);
			var sourceText = SourceText.From(source);
			GeneratorHost.RunGenerator(sourceText, new RoutedEventGenerator());
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
