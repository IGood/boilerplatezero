﻿// Copyright © Ian Good

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Bpz.Wpf
{
	/// <summary>
	/// Represents a source generator that produces idiomatic code for WPF routed events.
	/// 
	/// <para>Looks for things like<br/>
	/// <c>public static readonly RoutedEvent FooChangedEvent = Gen.FooChanged<RoutedPropertyChangedEventHandler<int>>(RoutingStrategy.Direct);</c><br/>
	/// and generates the appropriate registration code.</para>
	/// </summary>
	[Generator]
	public class RoutedEventGenerator : ISourceGenerator
	{
		private const string HelpLinkUri = "https://github.com/IGood/boilerplatezero#readme";

		/// <summary>
		/// Whether the generated code should be null-aware (i.e. the nullable annotation context is enabled).
		/// </summary>
		private bool useNullableContext;

		// These will be initialized before first use.
		private INamedTypeSymbol rehTypeSymbol = null!;  // System.Windows.RoutedEventHandler
		private INamedTypeSymbol rpcehTypeSymbol = null!;// System.Windows.RoutedPropertyChangedEventHandler<>

		public void Initialize(GeneratorInitializationContext context)
		{
			//DebugMe.Go();
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			//DebugMe.Go();

			this.useNullableContext = (context.ParseOptions as CSharpParseOptions)?.LanguageVersion >= LanguageVersion.CSharp8;

			var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver!;

			// Cast keys to `ISymbol` in the key selector to make the analyzer shutup about CS8602 ("Dereference of a possibly null reference.").
			var namespaces = UpdateAndFilterGenerationRequests(context, syntaxReceiver.GenerationRequests)
			   .GroupBy(g => (ISymbol)g.FieldSymbol.ContainingType, SymbolEqualityComparer.Default)
			   .GroupBy(g => (ISymbol)g.Key.ContainingNamespace, SymbolEqualityComparer.Default);

			StringBuilder sourceBuilder = new();

			foreach (var namespaceGroup in namespaces)
			{
				// Get these type symbols now so we don't waste time finding them each time we need them later.
				this.rehTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Windows.RoutedEventHandler")!;
				this.rpcehTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Windows.RoutedPropertyChangedEventHandler`1")!;

				string namespaceName = namespaceGroup.Key.ToString();
				sourceBuilder.Append($@"
namespace {namespaceName}
{{");

				foreach (var classGroup in namespaceGroup)
				{
					string? maybeStatic = classGroup.Key.IsStatic ? "static " : null;
					string className = GetTypeName((INamedTypeSymbol)classGroup.Key);
					sourceBuilder.Append($@"
	{maybeStatic}partial class {className}
	{{");

					foreach (var generateThis in classGroup)
					{
						context.CancellationToken.ThrowIfCancellationRequested();

						this.ApppendSource(context, sourceBuilder, generateThis);
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
				string? maybeNullableContext = this.useNullableContext ? "#nullable enable" : null;

				sourceBuilder.Insert(0,
$@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a boilerplatezero (BPZ) source generator.
//     Generator = {this.GetType().FullName}
//     {HelpLinkUri}
// </auto-generated>
//------------------------------------------------------------------------------
{maybeNullableContext}
using System.Windows;
");

				context.AddSource($"bpz.RoutedEvents.g.cs", sourceBuilder.ToString());
			}
		}

		private void ApppendSource(GeneratorExecutionContext context, StringBuilder sourceBuilder, GenerationDetails generateThis)
		{
			string eventName = generateThis.MethodNameNode.Identifier.ValueText;
			string routedEventMemberName = generateThis.FieldSymbol.Name;

			// Try to get the generic type argument (if it exists, this will be the type of the event handler).
			ITypeSymbol? genTypeArg = null;
			if (generateThis.MethodNameNode is GenericNameSyntax genMethodNameNode)
			{
				var typeArgNode = genMethodNameNode.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgNode != null)
				{
					var model = context.Compilation.GetSemanticModel(typeArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(typeArgNode, context.CancellationToken);
					genTypeArg = typeInfo.Type;

					// A nullable ref type like `string?` loses its annotation here. Let's put it back.
					// Note: Nullable value types like `int?` do not have this issue.
					if (genTypeArg != null &&
						genTypeArg.IsReferenceType &&
						typeArgNode is NullableTypeSyntax)
					{
						genTypeArg = genTypeArg.WithNullableAnnotation(NullableAnnotation.Annotated);
					}
				}
			}

			// Determine the type of the handler.
			// If there is a generic type argument, then use that; otherwise, use `RoutedEventHandler`.
			generateThis.EventHandlerType = genTypeArg ?? this.rehTypeSymbol;
			generateThis.EventHandlerTypeName = generateThis.EventHandlerType.ToDisplayString();

			string genClassDecl;
			string? moreDox = null;

			if (generateThis.IsAttached)
			{
				string targetTypeName = "DependencyObject";
				string callerExpression = "(d as UIElement)?";

				if (generateThis.MethodNameNode.Parent is MemberAccessExpressionSyntax memberAccessExpr &&
					memberAccessExpr.Expression is GenericNameSyntax genClassNameNode)
				{
					genClassDecl = "GenAttached<__TTarget> where __TTarget : DependencyObject";

					var typeArgNode = genClassNameNode.TypeArgumentList.Arguments.FirstOrDefault();
					if (typeArgNode != null)
					{
						var model = context.Compilation.GetSemanticModel(typeArgNode.SyntaxTree);
						generateThis.AttachmentNarrowingType = model.GetTypeInfo(typeArgNode, context.CancellationToken).Type;
						if (generateThis.AttachmentNarrowingType != null)
						{
							targetTypeName = generateThis.AttachmentNarrowingType.ToDisplayString();
							callerExpression = "d";
							moreDox = $@"<br/>This attached event is only for use with objects of type <see cref=""{ReplaceBrackets(targetTypeName)}""/>.";
						}
					}
				}
				else
				{
					genClassDecl = "GenAttached";
				}

				// Write the static get/set methods source code.
				string methodsAccess = generateThis.FieldSymbol.DeclaredAccessibility.ToString().ToLower();

				// Something like...
				//	public static void AddFooChangedHandler(DependencyObject d, RoutedPropertyChangedEventHandler<int> handler) => (d as UIElement)?.AddHandler(FooChangedEvent, handler);
				//	public static void RemoveFooChangedHandler(DependencyObject d, RoutedPropertyChangedEventHandler<int> handler) => (d as UIElement)?.RemoveHandler(FooChangedEvent, handler);
				sourceBuilder.Append($@"
		{methodsAccess} static void Add{eventName}Handler({targetTypeName} d, {generateThis.EventHandlerTypeName} handler) => {callerExpression}.AddHandler({routedEventMemberName}, handler);
		{methodsAccess} static void Remove{eventName}Handler({targetTypeName} d, {generateThis.EventHandlerTypeName} handler) => {callerExpression}.RemoveHandler({routedEventMemberName}, handler);");
			}
			else
			{
				genClassDecl = "Gen";

				// Let's include the documentation because that's nice.
				string? maybeDox = null;
				if (TryGetAncestor(generateThis.MethodNameNode, out FieldDeclarationSyntax? fieldDeclNode))
				{
					maybeDox = fieldDeclNode
						.DescendantTrivia()
						.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
						.ToFullString();
					if (maybeDox.Length != 0)
					{
						maybeDox += "\t\t";
					}
				}

				// Write the instance event source code.
				string eventAccess = generateThis.FieldSymbol.DeclaredAccessibility.ToString().ToLower();

				// Something like...
				//	public event RoutedPropertyChangedEventHandler<int> FooChanged
				//	{
				//		add => this.AddHandler(FooChangedEvent, value);
				//		remove => this.RemoveHandler(FooChangedEvent, value);
				//	}
				sourceBuilder.Append($@"
		{maybeDox}{eventAccess} event {generateThis.EventHandlerTypeName} {eventName}
		{{
			add => this.AddHandler({routedEventMemberName}, value);
			remove => this.RemoveHandler({routedEventMemberName}, value);
		}}");
			}

			// Write the static helper method.
			string what = generateThis.IsAttached ? "an attached event" : "a routed event";

			string maybeGeneric, maybeGenericConstraint, handlerTypeName;
			if (genTypeArg != null)
			{
				maybeGeneric = "<__T>";
				maybeGenericConstraint = " where __T : System.Delegate";
				handlerTypeName = "__T";
			}
			else
			{
				maybeGeneric = "";
				maybeGenericConstraint = "";
				handlerTypeName = generateThis.EventHandlerTypeName;
			}

			string ownerTypeName = GetTypeName(generateThis.FieldSymbol.ContainingType);

			sourceBuilder.Append($@"
		private static partial class {genClassDecl}
		{{
			/// <summary>
			/// Registers {what} named ""{eventName}"" whose handler type is <see cref=""{ReplaceBrackets(generateThis.EventHandlerTypeName)}""/>.{moreDox}
			/// </summary>
			public static RoutedEvent {eventName}{maybeGeneric}(RoutingStrategy routingStrategy = RoutingStrategy.Direct){maybeGenericConstraint}
			{{
				return EventManager.RegisterRoutedEvent(""{eventName}"", routingStrategy, typeof({handlerTypeName}), typeof({ownerTypeName}));
			}}
		}}
");
		}

		/// <summary>
		/// Inspects candidates for correctness and updates them with additional information.
		/// Yields those which satisfy requirements for code generation.
		/// </summary>
		private static IEnumerable<GenerationDetails> UpdateAndFilterGenerationRequests(GeneratorExecutionContext context, IEnumerable<GenerationDetails> requests)
		{
			INamedTypeSymbol? reTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.RoutedEvent");

			foreach (var gd in requests)
			{
				var model = context.Compilation.GetSemanticModel(gd.MethodNameNode.SyntaxTree);
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
						context.ReportDiagnostic(Diagnostics.UnexpectedFieldType(fieldSymbol));
					}
				}
			}
		}

		/// <summary>
		/// Traverses the syntax tree upward from <paramref name="syntaxNode"/> and returns the first node of type
		/// <typeparamref name="T"/> if one exists.
		/// </summary>
		private static bool TryGetAncestor<T>(SyntaxNode syntaxNode, [NotNullWhen(true)] out T? ancestorSyntaxNode) where T : SyntaxNode
		{
			if (syntaxNode.Parent != null)
			{
				if (syntaxNode.Parent is T found)
				{
					ancestorSyntaxNode = found;
					return true;
				}

				return TryGetAncestor(syntaxNode.Parent, out ancestorSyntaxNode);
			}

			ancestorSyntaxNode = null;
			return false;
		}

		/// <summary>
		/// Gets the name of the type including generic type parameters (ex: "Widget&lt;TSomething&gt;").
		/// </summary>
		private static string GetTypeName(INamedTypeSymbol typeSymbol)
		{
			if (typeSymbol.IsGenericType)
			{
				string name = typeSymbol.ToDisplayString();
				int indexOfAngle = name.IndexOf('<');
				int indexOfDot = name.LastIndexOf('.', indexOfAngle);
				return name.Substring(indexOfDot + 1);
			}

			return typeSymbol.Name;
		}

		/// <summary>
		/// Replaces angle brackets ("&lt;&gt;") with curly braces ("{}").
		/// </summary>
		private static string ReplaceBrackets(string typeName)
		{
			int indexOfBracket = typeName.IndexOf('<');
			if (indexOfBracket < 0)
			{
				return typeName;
			}

			char[] chars = typeName.ToCharArray();

			for (int i = indexOfBracket; i < chars.Length; ++i)
			{
				ref char c = ref chars[i];
				if (c == '<') c = '{';
				else if (c == '>') c = '}';
			}

			return new string(chars);
		}

		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<GenerationDetails> GenerationRequests { get; } = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
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
					if (fieldTypeName.LastIndexOf("RoutedEvent", StringComparison.Ordinal) >= 0)
					{
						// Looking for field initialization like "= Gen.FooChanged"...
						var varDecl = fieldDecl.Declaration.Variables.FirstOrDefault();
						if (varDecl?.Initializer?.Value is InvocationExpressionSyntax invocationExpr &&
							invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
							memberAccessExpr.Expression is SimpleNameSyntax idName)
						{
							if (idName.Identifier.ValueText == "Gen")
							{
								this.GenerationRequests.Add(new(memberAccessExpr.Name, false));
							}
							else if (idName.Identifier.ValueText == "GenAttached")
							{
								this.GenerationRequests.Add(new(memberAccessExpr.Name, true));
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Represents a candidate routed event for which source may be generated.
		/// </summary>
		private class GenerationDetails
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
			/// Gets or sets the optional type used to restrict the target type of the attached property.
			/// For instance, <c>System.Windows.Controls.Button</c> can be specified such that the attached property may
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
		}

		private static class Diagnostics
		{
			private static readonly DiagnosticDescriptor MismatchedIdentifiersError = new(
				id: "BPZ0001",
				title: "Mismatched identifiers",
				messageFormat: "Field name '{0}' and method name '{1}' do not match. Expected '{2} = {3}'.",
				category: "Naming",
				defaultSeverity: DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: null,
				helpLinkUri: HelpLinkUri,
				customTags: WellKnownDiagnosticTags.Compiler);

			public static Diagnostic MismatchedIdentifiers(IFieldSymbol fieldSymbol, string methodName, string expectedFieldName, string initializer)
			{
				return Diagnostic.Create(
					descriptor: MismatchedIdentifiersError,
					location: fieldSymbol.Locations[0],
					fieldSymbol.Name,
					methodName,
					expectedFieldName,
					initializer);
			}

			private static readonly DiagnosticDescriptor UnexpectedFieldTypeError = new(
				id: "BPZ1002",
				title: "Unexpected field type",
				messageFormat: "'{0}.{1}' has unexpected type '{2}'. Expected 'System.Windows.RoutedEvent'.",
				category: "Types",
				defaultSeverity: DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: null,
				helpLinkUri: HelpLinkUri,
				customTags: WellKnownDiagnosticTags.Compiler);

			public static Diagnostic UnexpectedFieldType(IFieldSymbol fieldSymbol)
			{
				return Diagnostic.Create(
					descriptor: UnexpectedFieldTypeError,
					location: fieldSymbol.Locations[0],
					fieldSymbol.ContainingType.Name,
					fieldSymbol.Name,
					fieldSymbol.Type.ToDisplayString());
			}
		}
	}
}
