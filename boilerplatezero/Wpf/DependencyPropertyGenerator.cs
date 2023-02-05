﻿// Copyright © Ian Good

using Bpz.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Bpz.Wpf;

/// <summary>
/// Represents a source generator that produces idiomatic code for WPF dependency properties.
/// 
/// <para>Looks for things like<br/>
/// <c>public static readonly DependencyProperty FooProperty = Gen.Foo(123);</c><br/>
/// and generates the appropriate registration and getter/setter code.</para>
/// 
/// Property-changed handlers with appropriate names and compatible signatures like<br/>
/// <c>private static void FooPropertyChanged(MyClass self, DependencyPropertyChangedEventArgs e) { ... }</c><br/>
/// will be included in the registration.
/// </summary>
[Generator(LanguageNames.CSharp)]
public partial class DependencyPropertyGenerator : IIncrementalGenerator
{
	private string nullLiteral = "null";

	// These will be initialized before first use.
	private INamedTypeSymbol objTypeSymbol = null!; // System.Object
	private INamedTypeSymbol doTypeSymbol = null!;  // System.Windows.DependencyObject
	private INamedTypeSymbol argsTypeSymbol = null!;// System.Windows.DependencyPropertyChangedEventArgs
	private INamedTypeSymbol? flagsTypeSymbol;      // System.Windows.FrameworkPropertyMetadataOptions
	private INamedTypeSymbol? reTypeSymbol;         // System.Windows.RoutedEvent

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//DebugMe.Go();

		// Whether the generated code should be null-aware (i.e. the nullable annotation context is enabled).
		var enableNullable = context
			.ParseOptionsProvider
			.Select(static (po, _) => (po as CSharpParseOptions)?.LanguageVersion >= LanguageVersion.CSharp8);

		var generationRequests = context
			.SyntaxProvider
			.CreateSyntaxProvider(IsSyntaxTargetForGeneration, CreateGenerationDetails);

		var source = enableNullable
			.Combine(context.CompilationProvider)
			.Combine(generationRequests.Collect());

		context.RegisterSourceOutput(
			source,
			(spc, x) => Execute(
				useNullableContext: x.Left.Left,
				compilation: x.Left.Right,
				generationRequests: x.Right,
				context: spc));
	}

	private void Execute(bool useNullableContext, Compilation compilation, ImmutableArray<GenerationDetails> generationRequests, SourceProductionContext context)
	{
		//DebugMe.Go();

		this.nullLiteral = useNullableContext ? "null!" : "null";

		// Get these type symbols now so we don't waste time finding them each time we need them later.
		this.objTypeSymbol ??= compilation.GetTypeByMetadataName("System.Object")!;
		this.doTypeSymbol ??= compilation.GetTypeByMetadataName("System.Windows.DependencyObject")!;
		this.argsTypeSymbol ??= compilation.GetTypeByMetadataName("System.Windows.DependencyPropertyChangedEventArgs")!;
		this.flagsTypeSymbol ??= compilation.GetTypeByMetadataName("System.Windows.FrameworkPropertyMetadataOptions");
		this.reTypeSymbol ??= compilation.GetTypeByMetadataName("System.Windows.RoutedEvent");

		// Cast keys to `ISymbol` in the key selector to make the analyzer shutup about CS8602 ("Dereference of a possibly null reference.").
		var namespaces = UpdateAndFilterGenerationRequests(context, compilation, generationRequests)
		   .GroupBy(g => (ISymbol)g.FieldSymbol.ContainingType, SymbolEqualityComparer.Default)
		   .GroupBy(g => (ISymbol)g.Key.ContainingNamespace, SymbolEqualityComparer.Default);

		StringBuilder sourceBuilder = new();

		foreach (var namespaceGroup in namespaces)
		{
			string namespaceName = namespaceGroup.Key.ToString();
			sourceBuilder.Append($@"
namespace {namespaceName}
{{");

			foreach (var classGroup in namespaceGroup)
			{
				string? maybeStatic = classGroup.Key.IsStatic ? "static " : null;
				string className = GeneratorOps.GetTypeName((INamedTypeSymbol)classGroup.Key);
				sourceBuilder.Append($@"
	{maybeStatic}partial class {className}
	{{");

				foreach (var generateThis in classGroup)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					this.ApppendSource(compilation, sourceBuilder, generateThis, context.CancellationToken);
				}

				sourceBuilder.Append(@"
	}
");
			}

			sourceBuilder.Append(@"
}
");
		}

		if (sourceBuilder.Length != 0)
		{
			string? maybeNullableContext = useNullableContext ? "#nullable enable" : null;

			sourceBuilder.Insert(0,
$@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a boilerplatezero (BPZ) source generator.
//     Generator = {this.GetType().FullName}
//     {Diagnostics.HelpLinkUri}
// </auto-generated>
//------------------------------------------------------------------------------
{maybeNullableContext}
using System.Windows;
");

			context.AddSource($"bpz.DependencyProperties.g.cs", sourceBuilder.ToString());
		}
	}
}
