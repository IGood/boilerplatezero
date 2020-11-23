﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Bpz.Wpf
{
	[Generator]
	public class DependencyPropertyGenerator : ISourceGenerator
	{
		private const string HelpLinkUri = "https://github.com/IGood/";

		public void Initialize(GeneratorInitializationContext context)
		{
			//DebugMe.Go();
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			//DebugMe.Go();
			var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver!;

			var namespaces = UpdateAndFilterGenerationRequests(context, syntaxReceiver.GenerationRequests)
				.GroupBy(g => g.FieldSymbol.ContainingType, SymbolEqualityComparer.Default)
				.GroupBy(g => g.Key.ContainingNamespace, SymbolEqualityComparer.Default);

			StringBuilder sourceBuilder = new StringBuilder();

			foreach (var namespaceGroup in namespaces)
			{
				string namespaceName = namespaceGroup.Key!.ToString();
				sourceBuilder.AppendLine($"namespace {namespaceName} {{");

				foreach (var classGroup in namespaceGroup)
				{
					string className = classGroup.Key!.Name;
					sourceBuilder.AppendLine($"\tpartial class {className} {{");

					foreach (var generateThis in classGroup)
					{
						ApppendSource(context, sourceBuilder, generateThis);
					}

					sourceBuilder.AppendLine("\t}");
				}

				sourceBuilder.AppendLine("}");
			}

			if (sourceBuilder.Length != 0)
			{
				sourceBuilder.Insert(0,
$@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by {this.GetType().FullName}
//     {HelpLinkUri}
// </auto-generated>
//------------------------------------------------------------------------------

using System.Windows;

");

				context.AddSource($"bpz.g.cs", sourceBuilder.ToString());
			}
		}

		private static void ApppendSource(GeneratorExecutionContext context, StringBuilder sourceBuilder, GenerationDetails generateThis)
		{
			string propertyName = generateThis.MethodNameNode.Identifier.ValueText;
			string dpMemberName = propertyName + "Property";
			string dpkMemberName = propertyName + "PropertyKey";

			Accessibility dpAccess = generateThis.FieldSymbol.DeclaredAccessibility;
			Accessibility dpkAccess = generateThis.FieldSymbol.DeclaredAccessibility;

			// If this is a DependencyPropertyKey, then we may need to create the corresponding DependencyProperty field.
			// The DependencyProperty field is required for TemplateBindings in XAML.
			if (generateThis.IsDpk)
			{
				ISymbol? dpMemberSymbol = generateThis.FieldSymbol.ContainingType.GetMembers(dpMemberName).FirstOrDefault();
				if (dpMemberSymbol != null)
				{
					dpAccess = dpMemberSymbol.DeclaredAccessibility;
				}
				else
				{
					dpAccess = Accessibility.Public;
					// Something like...
					//	public static readonly DependencyProperty FooProperty = FooPropertyKey.DependencyProperty;
					sourceBuilder.AppendLine($"\t\tpublic static readonly DependencyProperty {dpMemberName} = {dpkMemberName}.DependencyProperty;");
				}
			}

			// Get the default value argument if it exists.
			ArgumentSyntax? defaultValueArgNode = null;
			if (TryGetAncestor(generateThis.MethodNameNode, out InvocationExpressionSyntax? invocationExpressionNode))
			{
				defaultValueArgNode = invocationExpressionNode!.ArgumentList.Arguments.FirstOrDefault();
			}

			// Determine the type of the property.
			// If there are generic type arguments, then use that; otherwise, use the type of the default value argument.
			string? propertyTypeName = null;
			if (generateThis.MethodNameNode is GenericNameSyntax genMethodNameNode)
			{
				var typeArgNode = genMethodNameNode.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgNode != null)
				{
					var model = context.Compilation.GetSemanticModel(typeArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(typeArgNode, context.CancellationToken);
					propertyTypeName = typeInfo.Type?.ToDisplayString();
				}
			}
			else
			{
				if (defaultValueArgNode != null)
				{
					var model = context.Compilation.GetSemanticModel(defaultValueArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(defaultValueArgNode.Expression, context.CancellationToken);
					propertyTypeName = typeInfo.Type?.ToDisplayString();
				}
			}

			// This is just an extra safety precaution - ensures that the generated code is always valid.
			// But really, if we were unable to get the type, that means the user's code doesn't compile anyhow.
			propertyTypeName ??= "object";

			string genClassDecl;
			string? moreDox = null;

			if (generateThis.IsAttached)
			{
				string? targetTypeName = null;
				if (generateThis.MethodNameNode.Parent is MemberAccessExpressionSyntax memberAccessExpr &&
					memberAccessExpr.Expression is GenericNameSyntax genClassNameNode)
				{
					genClassDecl = "GenAttached<TTarget> where TTarget : DependencyObject";

					var typeArgNode = genClassNameNode.TypeArgumentList.Arguments.FirstOrDefault();
					if (typeArgNode != null)
					{
						var model = context.Compilation.GetSemanticModel(typeArgNode.SyntaxTree);
						var typeInfo = model.GetTypeInfo(typeArgNode, context.CancellationToken);
						targetTypeName = typeInfo.Type?.ToDisplayString();
						if (targetTypeName != null)
						{
							moreDox = $@"<br/>This attached property is only for use with objects of type <see cref=""{targetTypeName}""/>";
						}
					}
				}
				else
				{
					genClassDecl = "GenAttached";
				}

				targetTypeName ??= "DependencyObject";

				// Write the static get/set methods source code.
				string getterAccess = dpAccess.ToString().ToLower();
				string setterAccess = generateThis.IsDpk ? dpkAccess.ToString().ToLower() : getterAccess;
				string setterArg0 = generateThis.IsDpk ? dpkMemberName : dpMemberName;
				// Something like...
				//	public static int GetFoo(DependencyObject d) => (int)d.GetValue(FooProperty);
				//	private static void SetFoo(DependencyObject d, int value) => d.SetValue(FooPropertyKey);
				sourceBuilder.AppendLine(
$@"		{getterAccess} static {propertyTypeName} Get{propertyName}({targetTypeName} d) => ({propertyTypeName})d.GetValue({dpMemberName});
		{setterAccess} static void Set{propertyName}({targetTypeName} d, {propertyTypeName} value) => d.SetValue({setterArg0}, value);");
			}
			else
			{
				genClassDecl = "Gen";

				// Write the instance property source code.
				string propertyAccess = dpAccess.ToString().ToLower();
				string setterAccess = generateThis.IsDpk ? dpkAccess.ToString().ToLower() : "";
				string setterArg0 = generateThis.IsDpk ? dpkMemberName : dpMemberName;
				// Something like...
				//	public int Foo {
				//		get => (int)this.GetValue(FooProperty);
				//		set => this.SetValue(FooPropertyKey, value);
				//	}
				sourceBuilder.AppendLine(
$@"		{propertyAccess} {propertyTypeName} {propertyName} {{
			get => ({propertyTypeName})this.GetValue({dpMemberName});
			{setterAccess} set => this.SetValue({setterArg0}, value);
		}}");
			}

			// Write the static helper method.
			string what = generateThis.IsDpk
				? (generateThis.IsAttached ? "a read-only attached property" : "a read-only dependency property")
				: (generateThis.IsAttached ? "an attached property" : "a dependency property");
			string returnType = generateThis.FieldSymbol.Type.Name;
			bool hasDefaultValue = defaultValueArgNode != null;
			string parameter = hasDefaultValue ? "T defaultValue" : "";
			sourceBuilder.AppendLine(
$@"		private static partial class {genClassDecl} {{
			/// <summary>
			/// Registers {what} named ""{propertyName}"" whose type is <see cref=""{propertyTypeName}""/>.{moreDox}
			/// </summary>
			public static {returnType} {propertyName}<T>({parameter}) {{");

			if (hasDefaultValue)
			{
				sourceBuilder.AppendLine("PropertyMetadata typeMetadata = new PropertyMetadata(defaultValue);");
			}
			else
			{
				sourceBuilder.AppendLine("PropertyMetadata typeMetadata = null;");
			}

			string a = generateThis.IsAttached ? "Attached" : "";
			string ro = generateThis.IsDpk ? "ReadOnly" : "";
			string ownerTypeName = generateThis.FieldSymbol.ContainingType.Name;
			sourceBuilder.AppendLine(
$@"return DependencyProperty.Register{a}{ro}(""{propertyName}"", typeof(T), typeof({ownerTypeName}), typeMetadata);
			}}
		}}");
		}

		private static IEnumerable<GenerationDetails> UpdateAndFilterGenerationRequests(GeneratorExecutionContext context, IEnumerable<GenerationDetails> requests)
		{
			INamedTypeSymbol? dpTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyProperty");
			INamedTypeSymbol? dpkTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyPropertyKey");

			foreach (var gd in requests)
			{
				var model = context.Compilation.GetSemanticModel(gd.MethodNameNode.SyntaxTree);
				if (model.GetEnclosingSymbol(gd.MethodNameNode.SpanStart, context.CancellationToken) is IFieldSymbol fieldSymbol)
				{
					bool isDp = fieldSymbol.Type.Equals(dpTypeSymbol, SymbolEqualityComparer.Default);
					if (isDp || fieldSymbol.Type.Equals(dpkTypeSymbol, SymbolEqualityComparer.Default))
					{
						string methodName = gd.MethodNameNode.Identifier.ValueText;
						string expectedFieldName = methodName + (isDp ? "Property" : "PropertyKey");
						if (fieldSymbol.Name == expectedFieldName)
						{
							gd.FieldSymbol = fieldSymbol;
							gd.IsDpk = !isDp;
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

		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<GenerationDetails> GenerationRequests { get; } = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				// Looking for things like...
				//  public static readonly System.Windows.DependencyProperty FooProperty = Gen.Foo(123);
				//  public static readonly System.Windows.DependencyProperty BarProperty = GenAttached.Bar(123);
				if (syntaxNode is FieldDeclarationSyntax fieldDecl)
				{
					// Looking for "DependencyProperty" or "DependencyPropertyKey" as the type of the field...
					string fieldTypeName = fieldDecl.Declaration.Type.ToString();
					if (fieldTypeName.Contains("DependencyProperty"))
					{
						// Looking for field initialization like "= Gen.Foo"...
						var varDecl = fieldDecl.Declaration.Variables.FirstOrDefault();
						if (varDecl?.Initializer?.Value is InvocationExpressionSyntax invokationExpr &&
							invokationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
							memberAccessExpr.Expression is SimpleNameSyntax idName)
						{
							if (idName.Identifier.ValueText == "Gen")
							{
								this.GenerationRequests.Add(new GenerationDetails(memberAccessExpr.Name, false));
							}
							else if (idName.Identifier.ValueText == "GenAttached")
							{
								this.GenerationRequests.Add(new GenerationDetails(memberAccessExpr.Name, true));
							}
						}
					}
				}
			}
		}

		private class GenerationDetails
		{
			public GenerationDetails(SimpleNameSyntax methodNameNode, bool isAttached)
			{
				this.MethodNameNode = methodNameNode;
				this.IsAttached = isAttached;
			}

			public SimpleNameSyntax MethodNameNode { get; }
			public IFieldSymbol FieldSymbol { get; set; } = null!;
			public bool IsDpk { get; set; }
			public bool IsAttached { get; }
		}

		private static class Diagnostics
		{
			private static readonly DiagnosticDescriptor MismatchedIdentifiersDescriptor = new DiagnosticDescriptor(
				"BPZ0001",
				"Mismatched identifiers",
				"Field name '{0}' and method name '{1}' do not match. Expected '{2} = {3}'.",
				"Naming",
				DiagnosticSeverity.Error,
				true,
				null,
				HelpLinkUri,
				WellKnownDiagnosticTags.Compiler);

			public static Diagnostic MismatchedIdentifiers(IFieldSymbol fieldSymbol, string methodName, string expectedFieldName, string initializer)
			{
				return Diagnostic.Create(
					MismatchedIdentifiersDescriptor,
					fieldSymbol.Locations[0],
					fieldSymbol.Name,
					methodName,
					expectedFieldName,
					initializer);
			}

			private static readonly DiagnosticDescriptor UnexpectedFieldTypeDescriptor = new DiagnosticDescriptor(
				"BPZ1001",
				"Unexpected field type",
				"'{0}.{1}' has unexpected type '{2}'. Expected 'System.Windows.DependencyProperty' or 'System.Windows.DependencyPropertyKey'.",
				"Types",
				DiagnosticSeverity.Error,
				true,
				null,
				HelpLinkUri,
				WellKnownDiagnosticTags.Compiler);

			public static Diagnostic UnexpectedFieldType(IFieldSymbol fieldSymbol)
			{
				return Diagnostic.Create(
					UnexpectedFieldTypeDescriptor,
					fieldSymbol.Locations[0],
					fieldSymbol.ContainingType.Name,
					fieldSymbol.Name,
					fieldSymbol.Type.ToDisplayString());
			}
		}
	}
}
