// Copyright © Ian Good

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Windows;

namespace Bpz.Test
{
	public static class GeneratorHost
	{
		public static void RunGenerator(SourceText sourceText, ISourceGenerator generator)
		{
			var compilation = CSharpCompilation
				.Create("MyTestAssembly")
				.WithOptions(new(OutputKind.DynamicallyLinkedLibrary))
				.WithReferences(
					MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(DependencyObject).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(FrameworkPropertyMetadataOptions).Assembly.Location),
					MetadataReference.CreateFromFile(typeof(RoutedEvent).Assembly.Location))
				.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceText));

			var rr = CSharpGeneratorDriver
				.Create(generator)
				.RunGenerators(compilation)
				.GetRunResult();

			foreach (var result in rr.Results)
			{
				foreach (var generatedSourceResult in result.GeneratedSources)
				{
					Console.WriteLine(generatedSourceResult.SyntaxTree.ToString());
				}
			}
		}
	}
}
