// Copyright © Ian Good

using Bpz.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Bpz.Wpf;

public partial class RoutedEventGenerator
{
	/// <summary>
	/// Inspects candidates for correctness and updates them with additional information.
	/// Yields those which satisfy requirements for code generation.
	/// </summary>
	private static IEnumerable<GenerationDetails> UpdateAndFilterGenerationRequests(SourceProductionContext context, Compilation compilation, IEnumerable<GenerationDetails> requests)
	{
		INamedTypeSymbol? reTypeSymbol = compilation.GetTypeByMetadataName("System.Windows.RoutedEvent");
		if (reTypeSymbol == null)
		{
			// This probably never happens, but whatevs.
			yield break;
		}

		foreach (var gd in requests)
		{
			var model = compilation.GetSemanticModel(gd.MethodNameNode.SyntaxTree);
			if (model.GetEnclosingSymbol(gd.MethodNameNode.SpanStart, context.CancellationToken) is IFieldSymbol fieldSymbol &&
				fieldSymbol.IsStatic &&
				fieldSymbol.IsReadOnly)
			{
				if (fieldSymbol.Type.Equals(reTypeSymbol, SymbolEqualityComparer.Default))
				{
					string methodName = gd.MethodNameNode.Identifier.ValueText;
					string expectedFieldName = methodName + "Event";
					if (fieldSymbol.Name == expectedFieldName)
					{
						gd.FieldSymbol = fieldSymbol;
						yield return gd;
					}
					else
					{
						context.ReportDiagnostic(Diagnostics.MismatchedIdentifiers(fieldSymbol, methodName, expectedFieldName, gd.MethodNameNode.Parent!.ToString()));
					}
				}
				else
				{
					context.ReportDiagnostic(Diagnostics.UnexpectedFieldType(fieldSymbol, reTypeSymbol));
				}
			}
		}
	}

	private static bool IsSyntaxTargetForGeneration(SyntaxNode syntaxNode, CancellationToken _)
	{
		// Looking for things like...
		//	public static readonly System.Windows.RoutedEvent FooChangedEvent = Gen.FooChanged<RoutedPropertyChangedEventHandler<int>>(RoutingStrategy.Direct);
		//	public static readonly System.Windows.RoutedEvent FooChangedEvent = Gen.FooChanged<int>(RoutingStrategy.Direct);
		//	public static readonly System.Windows.RoutedEvent BarUpdatedEvent = GenAttached.BarUpdated<RoutedEventHandler>(RoutingStrategy.Bubble);
		//	public static readonly System.Windows.RoutedEvent BarUpdatedEvent = GenAttached.BarUpdated(RoutingStrategy.Bubble);
		if (syntaxNode is FieldDeclarationSyntax fieldDecl)
		{
			// Looking for "RoutedEvent" as the type of the field...
			string fieldTypeName = fieldDecl.Declaration.Type.ToString();
			if (fieldTypeName.EndsWith("RoutedEvent", StringComparison.Ordinal))
			{
				// Looking for field initialization like "= Gen.FooChanged"...
				var varDecl = fieldDecl.Declaration.Variables.FirstOrDefault();
				if (varDecl?.Initializer?.Value is InvocationExpressionSyntax invocationExpr &&
					invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
					memberAccessExpr.Expression is SimpleNameSyntax idName)
				{
					return idName.Identifier.ValueText == "Gen" || idName.Identifier.ValueText == "GenAttached";
				}
			}
		}

		return false;
	}

	private static GenerationDetails CreateGenerationDetails(GeneratorSyntaxContext context, CancellationToken _)
	{
		var fieldDecl = (FieldDeclarationSyntax)context.Node;

		// Looking for field initialization like "= Gen.FooChanged"...
		var varDecl = fieldDecl.Declaration.Variables[0];
		var invocationExpr = (InvocationExpressionSyntax)varDecl.Initializer!.Value;
		var memberAccessExpr = (MemberAccessExpressionSyntax)invocationExpr.Expression;
		var idName = (SimpleNameSyntax)memberAccessExpr.Expression;
		return new(memberAccessExpr.Name, isAttached: idName.Identifier.ValueText == "GenAttached");
	}

	/// <summary>
	/// Represents a candidate routed event for which source may be generated.
	/// </summary>
	private class GenerationDetails : IEquatable<GenerationDetails>
	{
		public GenerationDetails(SimpleNameSyntax methodNameNode, bool isAttached)
		{
			this.MethodNameNode = methodNameNode;
			this.IsAttached = isAttached;
		}

		/// <summary>
		/// Gets the syntax node representing the name of the method called to register the routed event.
		/// </summary>
		public SimpleNameSyntax MethodNameNode { get; }

		/// <summary>
		/// Gets the symbol representing the routed event field.
		/// </summary>
		public IFieldSymbol FieldSymbol { get; set; } = null!;

		/// <summary>
		/// Gets whether this is an attached event.
		/// </summary>
		public bool IsAttached { get; }

		/// <summary>
		/// Gets or sets the optional type used to restrict the target type of the attached event.
		/// For instance, <c>System.Windows.Controls.Button</c> can be specified such that the attached event may
		/// only be used on objects that derive from <c>Button</c>.
		/// </summary>
		public ITypeSymbol? AttachmentNarrowingType { get; set; }

		/// <summary>
		/// Gets or sets the type of the routed event handler.
		/// </summary>
		public ITypeSymbol? EventHandlerType { get; set; }

		/// <summary>
		/// Gets or sets the name of the type of the routed event handler.
		/// </summary>
		public string EventHandlerTypeName { get; set; } = "RoutedEventHandler";

		/// <inheritdoc/>
		public bool Equals(GenerationDetails other) => this.IsAttached == other.IsAttached && this.MethodNameNode.IsEquivalentTo(other.MethodNameNode);
	}
}
