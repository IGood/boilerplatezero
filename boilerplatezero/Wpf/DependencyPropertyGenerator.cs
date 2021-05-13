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
	[Generator]
	public class DependencyPropertyGenerator : ISourceGenerator
	{
		private const string HelpLinkUri = "https://github.com/IGood/boilerplatezero#readme";

		/// <summary>
		/// Whether the generated code should be null-aware (i.e. the nullable annotation context is enabled).
		/// </summary>
		private bool useNullableContext;

		// These will be initialized before first.
		private INamedTypeSymbol objTypeSymbol = null!; // System.Object
		private INamedTypeSymbol doTypeSymbol = null!;  // System.Windows.DependencyObject
		private INamedTypeSymbol argsTypeSymbol = null!;// System.Windows.DependencyPropertyChangedEventArgs

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
				this.objTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Object")!;
				this.doTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Windows.DependencyObject")!;
				this.argsTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Windows.DependencyPropertyChangedEventArgs")!;

				string namespaceName = namespaceGroup.Key.ToString();
				sourceBuilder.AppendLine($"namespace {namespaceName} {{");

				foreach (var classGroup in namespaceGroup)
				{
					string? maybeStatic = classGroup.Key.IsStatic ? "static" : null;
					string className = GetTypeName((INamedTypeSymbol)classGroup.Key);
					sourceBuilder.AppendLine($"\t{maybeStatic} partial class {className} {{");

					foreach (var generateThis in classGroup)
					{
						this.ApppendSource(context, sourceBuilder, generateThis);
					}

					sourceBuilder.AppendLine("\t}");
				}

				sourceBuilder.AppendLine("}");
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

				context.AddSource($"bpz.DependencyProperties.g.cs", sourceBuilder.ToString());
			}
		}

		private void ApppendSource(GeneratorExecutionContext context, StringBuilder sourceBuilder, GenerationDetails generateThis)
		{
			string propertyName = generateThis.MethodNameNode.Identifier.ValueText;
			string dpMemberName = propertyName + "Property";
			string dpkMemberName = propertyName + "PropertyKey";

			Accessibility dpAccess = generateThis.FieldSymbol.DeclaredAccessibility;
			Accessibility dpkAccess = generateThis.FieldSymbol.DeclaredAccessibility;

			// If this is a DependencyPropertyKey, then we may need to create the corresponding DependencyProperty field.
			// We do this because it's proper to always have a DependencyProperty field & because the DependencyProperty
			// field is required when using TemplateBindings in XAML.
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

			// TODO: Check for a FrameworkPropertyMetadataOptions argument.

			// Get the default value argument if it exists.
			ArgumentSyntax? defaultValueArgNode = null;
			if (TryGetAncestor(generateThis.MethodNameNode, out InvocationExpressionSyntax? invocationExpressionNode))
			{
				defaultValueArgNode = invocationExpressionNode!.ArgumentList.Arguments.FirstOrDefault();
			}

			// Determine the type of the property.
			// If there are generic type arguments, then use that; otherwise, use the type of the default value argument.
			// As a safety precaution - ensure that the generated code is always valid by defaulting to use `object`.
			// But really, if we were unable to get the type, that means the user's code doesn't compile anyhow.
			generateThis.PropertyType = this.objTypeSymbol;
			if (generateThis.MethodNameNode is GenericNameSyntax genMethodNameNode)
			{
				var typeArgNode = genMethodNameNode.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgNode != null)
				{
					var model = context.Compilation.GetSemanticModel(typeArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(typeArgNode, context.CancellationToken);
					generateThis.PropertyType = typeInfo.Type ?? this.objTypeSymbol;

					// A nullable ref type like `string?` loses its annotation here. Let's put it back.
					// Note: Nullable value types like `int?` do not have this issue.
					if (generateThis.PropertyType.IsReferenceType && typeArgNode is NullableTypeSyntax)
					{
						generateThis.PropertyType = generateThis.PropertyType.WithNullableAnnotation(NullableAnnotation.Annotated);
					}
				}
			}
			else
			{
				if (defaultValueArgNode != null)
				{
					var model = context.Compilation.GetSemanticModel(defaultValueArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(defaultValueArgNode.Expression, context.CancellationToken);
					generateThis.PropertyType = typeInfo.Type ?? this.objTypeSymbol;

					// Handle default value expressions like `(string?)null`.
					// A nullable ref type like `string?` loses its annotation here. Let's put it back.
					// Note: Nullable value types like `int?` do not have this issue.
					if (generateThis.PropertyType.IsReferenceType &&
						defaultValueArgNode.Expression is CastExpressionSyntax castNode &&
						castNode.Type is NullableTypeSyntax)
					{
						generateThis.PropertyType = generateThis.PropertyType.WithNullableAnnotation(NullableAnnotation.Annotated);
					}
				}
			}

			generateThis.PropertyTypeName = generateThis.PropertyType.ToDisplayString();

			string genClassDecl;
			string? moreDox = null;

			if (generateThis.IsAttached)
			{
				string targetTypeName = "DependencyObject";
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
							moreDox = $@"<br/>This attached property is only for use with objects of type <see cref=""{ReplaceBrackets(targetTypeName)}""/>.";
						}
					}
				}
				else
				{
					genClassDecl = "GenAttached";
				}

				// Write the static get/set methods source code.
				string getterAccess = dpAccess.ToString().ToLower();
				string setterAccess = generateThis.IsDpk ? dpkAccess.ToString().ToLower() : getterAccess;
				string setterArg0 = generateThis.IsDpk ? dpkMemberName : dpMemberName;

				// Something like...
				//	public static int GetFoo(DependencyObject d) => (int)d.GetValue(FooProperty);
				//	private static void SetFoo(DependencyObject d, int value) => d.SetValue(FooPropertyKey);
				sourceBuilder.AppendLine(
$@"		{getterAccess} static {generateThis.PropertyTypeName} Get{propertyName}({targetTypeName} d) => ({generateThis.PropertyTypeName})d.GetValue({dpMemberName});
		{setterAccess} static void Set{propertyName}({targetTypeName} d, {generateThis.PropertyTypeName} value) => d.SetValue({setterArg0}, value);");
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
$@"		{maybeDox}{propertyAccess} {generateThis.PropertyTypeName} {propertyName} {{
			get => ({generateThis.PropertyTypeName})this.GetValue({dpMemberName});
			{setterAccess} set => this.SetValue({setterArg0}, value);
		}}");
			}

			// Write the static helper method.
			string what = generateThis.IsDpk
				? (generateThis.IsAttached ? "a read-only attached property" : "a read-only dependency property")
				: (generateThis.IsAttached ? "an attached property" : "a dependency property");
			string returnType = generateThis.FieldSymbol.Type.Name;
			bool hasDefaultValue = defaultValueArgNode != null;
			string parameter = hasDefaultValue ? "__T defaultValue" : "";
			sourceBuilder.AppendLine(
$@"		private static partial class {genClassDecl} {{
			/// <summary>
			/// Registers {what} named ""{propertyName}"" whose type is <see cref=""{ReplaceBrackets(generateThis.PropertyTypeName)}""/>.{moreDox}
			/// </summary>
			public static {returnType} {propertyName}<__T>({parameter}) {{");

			this.ApppendPropertyMetadata(sourceBuilder, generateThis, hasDefaultValue);

			string a = generateThis.IsAttached ? "Attached" : "";
			string ro = generateThis.IsDpk ? "ReadOnly" : "";
			string ownerTypeName = GetTypeName(generateThis.FieldSymbol.ContainingType);
			sourceBuilder.AppendLine(
$@"return DependencyProperty.Register{a}{ro}(""{propertyName}"", typeof(__T), typeof({ownerTypeName}), metadata);
			}}
		}}");
		}

		/// <summary>
		/// Appends source text that declares the property metadata variable named "metadata".
		/// Accounts for whether a default value exists.
		/// Accounts for whether a compatible property-changed handler exists.
		/// Accounts for whether a compatible coercion handler exists.
		/// </summary>
		private void ApppendPropertyMetadata(StringBuilder sourceBuilder, GenerationDetails generateThis, bool hasDefaultValue)
		{
			INamedTypeSymbol ownerType = generateThis.FieldSymbol.ContainingType;
			string propertyName = generateThis.MethodNameNode.Identifier.ValueText;
			string coerceMethodName = "Coerce" + propertyName;

			AssociatedMethods foundMethods = AssociatedMethods.None;

			string changeHandler = "null";
			string coercionHandler = "null";

			// Look for associated methods...
			foreach (ISymbol memberSymbol in ownerType.GetMembers())
			{
				if (memberSymbol.Kind == SymbolKind.Method)
				{
					if (!foundMethods.HasFlag(AssociatedMethods.PropertyChanged) && _TryGetChangeHandler((IMethodSymbol)memberSymbol, out changeHandler))
					{
						foundMethods |= AssociatedMethods.PropertyChanged;
						if (foundMethods == AssociatedMethods.All)
						{
							break;
						}

						continue;
					}

					if (!foundMethods.HasFlag(AssociatedMethods.Coerce) && _TryGetCoercionHandler((IMethodSymbol)memberSymbol, out coercionHandler))
					{
						foundMethods |= AssociatedMethods.Coerce;
						if (foundMethods == AssociatedMethods.All)
						{
							break;
						}
					}
				}
			}

			// See if we have any property-changed handlers like...
			//	static void FooPropertyChanged(Widget self, DependencyPropertyChangedEventArgs e) { ... }
			//	static void OnFooChanged(Widget self, DependencyPropertyChangedEventArgs e) { ... }
			//	void FooChanged(DependencyPropertyChangedEventArgs e) { ... }
			//	void OnFooChanged(string oldFoo, string newFoo) { ... }
			bool _TryGetChangeHandler(IMethodSymbol methodSymbol, out string changeHandler)
			{
				if (methodSymbol.ReturnsVoid)
				{
					string methodName = methodSymbol.Name;

					if (methodSymbol.IsStatic)
					{
						if (methodSymbol.Parameters.Length == 2 &&
							methodName.EndsWith("Changed", StringComparison.Ordinal) &&
							methodName.IndexOf(propertyName, 0, methodName.Length - "Changed".Length, StringComparison.Ordinal) >= 0)
						{
							ITypeSymbol p0TypeSymbol = methodSymbol.Parameters[0].Type;
							ITypeSymbol p1TypeSymbol = methodSymbol.Parameters[1].Type;

							if (p1TypeSymbol.Equals(argsTypeSymbol, SymbolEqualityComparer.Default))
							{
								if (p0TypeSymbol.Equals(doTypeSymbol, SymbolEqualityComparer.Default))
								{
									// Signature matches `System.Windows.PropertyChangedCallback`, so we can just use the method name.
									changeHandler = methodSymbol.Name;
									return true;
								}

								// Need to ensure type of p0 is valid.
								ITypeSymbol derivedTypeSymbol;
								if (generateThis.IsAttached)
								{
									// Narrowing type must be equal to, or derived from, the p0 type.
									derivedTypeSymbol = generateThis.AttachmentNarrowingType ?? doTypeSymbol;
								}
								else
								{
									// Owner type must be equal to, or derived from, the p0 type.
									derivedTypeSymbol = ownerType;
								}

								if (CanCastTo(derivedTypeSymbol, p0TypeSymbol))
								{
									// Something like...
									//	(d, e) => FooPropertyChanged((Goodies.Widget)d, e)
									changeHandler = $"(d, e) => {methodName}(({p0TypeSymbol.ToDisplayString()})d, e)";
									return true;
								}
							}
						}
					}
					// Not `static`:
					else if (!generateThis.IsAttached && (methodName == $"On{propertyName}Changed" || methodName == $"{propertyName}Changed"))
					{
						// Instance methods with 2 parameters look like...
						//	void OnFooChanged(int oldFoo, int newFoo) { ... }
						if (methodSymbol.Parameters.Length == 2)
						{
							IParameterSymbol p0 = methodSymbol.Parameters[0];
							IParameterSymbol p1 = methodSymbol.Parameters[1];

							if (p0.Type.Equals(p1.Type, SymbolEqualityComparer.Default) &&
								p0.Type.Equals(generateThis.PropertyType, SymbolEqualityComparer.Default) &&
								p0.Name.StartsWith("old", StringComparison.OrdinalIgnoreCase) &&
								p1.Name.StartsWith("new", StringComparison.OrdinalIgnoreCase))
							{
								string? maybeCastArgs = (generateThis.PropertyType.SpecialType != SpecialType.System_Object)
									? $"({generateThis.PropertyTypeName})"
									: null;

								// Something like...
								//	(d, e) => ((Goodies.Widget)d).OnFooChanged((int)e.OldValue, (int)e.NewValue)
								changeHandler = $"(d, e) => (({ownerType.ToDisplayString()})d).{methodName}({maybeCastArgs}e.OldValue, {maybeCastArgs}e.NewValue)";
								return true;
							}
						}
						// Instance methods with 1 parameter look like...
						//	void OnFooChanged(DependencyPropertyChangedEventArgs e) { ... }
						else if (methodSymbol.Parameters.Length == 1)
						{
							ITypeSymbol p0TypeSymbol = methodSymbol.Parameters[0].Type;

							if (p0TypeSymbol.Equals(argsTypeSymbol, SymbolEqualityComparer.Default))
							{
								// Something like...
								//	(d, e) => ((Goodies.Widget)d).OnFooChanged(e)
								changeHandler = $"(d, e) => (({ownerType.ToDisplayString()})d).{methodName}(e)";
								return true;
							}
						}
					}
				}

				changeHandler = "null";
				return false;
			}

			// See if we have any coercion handlers like...
			//	static object CoerceFoo(DependencyObject d, object baseValue) { ... }
			//	static int CoerceFoo(Widget self, int baseValue) { ... }
			bool _TryGetCoercionHandler(IMethodSymbol methodSymbol, out string coercionHandler)
			{
				string methodName = methodSymbol.Name;
				if (methodSymbol.IsStatic &&
					!methodSymbol.ReturnsVoid &&
					methodSymbol.Parameters.Length == 2 &&
					methodName == coerceMethodName)
				{
					bool requireLambda = false;

					// Ensure return type is valid. Must be `object` or the property type.
					ITypeSymbol retTypeSymbol = methodSymbol.ReturnType;
					if (retTypeSymbol.SpecialType != SpecialType.System_Object)
					{
						if (!retTypeSymbol.Equals(generateThis.PropertyType, SymbolEqualityComparer.Default))
						{
							coercionHandler = "null";
							return false;
						}

						// If the return type is a value type, then we must generate a lambda to call the method;
						// otherwise, the method may be compatible with `System.Windows.CoerceValueCallback` as is.
						requireLambda = retTypeSymbol.IsValueType;
					}

					// Ensure type of p0 is valid. Must be `DependencyObject` or compatible with the owner type.
					string? maybeCastArg0 = null;
					ITypeSymbol p0TypeSymbol = methodSymbol.Parameters[0].Type;
					if (!p0TypeSymbol.Equals(doTypeSymbol, SymbolEqualityComparer.Default))
					{
						ITypeSymbol derivedTypeSymbol;
						if (generateThis.IsAttached)
						{
							// Narrowing type must be equal to, or derived from, the p0 type.
							derivedTypeSymbol = generateThis.AttachmentNarrowingType ?? doTypeSymbol;
						}
						else
						{
							// Owner type must be equal to, or derived from, the p0 type.
							derivedTypeSymbol = ownerType;
						}

						if (!CanCastTo(derivedTypeSymbol, p0TypeSymbol))
						{
							coercionHandler = "null";
							return false;
						}

						requireLambda = true;
						maybeCastArg0 = $"({p0TypeSymbol.ToDisplayString()})";
					}

					// Ensure type of p1 is valid. Must be `object` or the property type.
					string? maybeCastArg1 = null;
					ITypeSymbol p1TypeSymbol = methodSymbol.Parameters[1].Type;
					if (p1TypeSymbol.SpecialType != SpecialType.System_Object)
					{
						if (!p1TypeSymbol.Equals(generateThis.PropertyType, SymbolEqualityComparer.Default))
						{
							coercionHandler = "null";
							return false;
						}

						requireLambda = true;
						maybeCastArg1 = $"({generateThis.PropertyTypeName})";
					}

					if (requireLambda)
					{
						// Something like...
						//	(d, baseValue) => CoerceFoo((Goodies.Widget)d, (int)baseValue)
						coercionHandler = $"(d, baseValue) => {methodName}({maybeCastArg0}d, {maybeCastArg1}baseValue)";
					}
					else
					{
						// Signature is compatible with `System.Windows.CoerceValueCallback`, so we can just use the method name.
						coercionHandler = methodName;
					}

					return true;
				}

				coercionHandler = "null";
				return false;
			}

			if (hasDefaultValue)
			{
				sourceBuilder.AppendLine($"PropertyMetadata metadata = new PropertyMetadata(defaultValue, {changeHandler}, {coercionHandler});");
				return;
			}

			if (changeHandler != "null")
			{
				sourceBuilder.AppendLine($"PropertyMetadata metadata = new PropertyMetadata({changeHandler}) {{ CoerceValueCallback = {coercionHandler} }};");
				return;
			}

			if (coercionHandler != "null")
			{
				sourceBuilder.AppendLine($"PropertyMetadata metadata = new PropertyMetadata() {{ CoerceValueCallback = {coercionHandler} }};");
				return;
			}

			string nullLiteral = this.useNullableContext ? "null!" : "null";
			sourceBuilder.AppendLine($"PropertyMetadata metadata = {nullLiteral};");
		}

		/// <summary>
		/// Inspects candidates for correctness and updates them with additional information.
		/// Yields those which satisfy requirements for code generation.
		/// </summary>
		private static IEnumerable<GenerationDetails> UpdateAndFilterGenerationRequests(GeneratorExecutionContext context, IEnumerable<GenerationDetails> requests)
		{
			INamedTypeSymbol? dpTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyProperty");
			INamedTypeSymbol? dpkTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyPropertyKey");

			foreach (var gd in requests)
			{
				var model = context.Compilation.GetSemanticModel(gd.MethodNameNode.SyntaxTree);
				if (model.GetEnclosingSymbol(gd.MethodNameNode.SpanStart, context.CancellationToken) is IFieldSymbol fieldSymbol &&
					fieldSymbol.IsStatic &&
					fieldSymbol.IsReadOnly)
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
		/// Returns <c>true</c> if <paramref name="checkThis"/> can be cast to <paramref name="baseTypeSymbol"/>;
		/// otherwise, returns <c>false</c>.
		/// </summary>
		private static bool CanCastTo(ITypeSymbol checkThis, ITypeSymbol baseTypeSymbol)
		{
			return checkThis.Equals(baseTypeSymbol, SymbolEqualityComparer.Default) || (checkThis.BaseType != null && CanCastTo(checkThis.BaseType, baseTypeSymbol));
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

		[Flags]
		private enum AssociatedMethods
		{
			None = 0,
			PropertyChanged = 1 << 0,
			Coerce = 1 << 1,
			All = PropertyChanged | Coerce,
		}

		private class SyntaxReceiver : ISyntaxReceiver
		{
			public List<GenerationDetails> GenerationRequests { get; } = new();

			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				// Looking for things like...
				//	public static readonly System.Windows.DependencyProperty FooProperty = Gen.Foo(123);
				//	public static readonly System.Windows.DependencyProperty BarProperty = GenAttached.Bar(123);
				if (syntaxNode is FieldDeclarationSyntax fieldDecl)
				{
					// Looking for "DependencyProperty" or "DependencyPropertyKey" as the type of the field...
					string fieldTypeName = fieldDecl.Declaration.Type.ToString();
					if (fieldTypeName.LastIndexOf("DependencyProperty", StringComparison.Ordinal) >= 0)
					{
						// Looking for field initialization like "= Gen.Foo"...
						var varDecl = fieldDecl.Declaration.Variables.FirstOrDefault();
						if (varDecl?.Initializer?.Value is InvocationExpressionSyntax invocationExpr &&
							invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
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

		/// <summary>
		/// Represents a candidate dependency property for which source may be generated.
		/// </summary>
		private class GenerationDetails
		{
			public GenerationDetails(SimpleNameSyntax methodNameNode, bool isAttached)
			{
				this.MethodNameNode = methodNameNode;
				this.IsAttached = isAttached;
			}

			/// <summary>
			/// Gets the syntax node representing the name of the method called to register the dependency property.
			/// </summary>
			public SimpleNameSyntax MethodNameNode { get; }

			/// <summary>
			/// Gets the symbol representing the dependency property (or dependency property key) field.
			/// </summary>
			public IFieldSymbol FieldSymbol { get; set; } = null!;

			/// <summary>
			/// Gets or sets a value indicating whether this is a dependency property key.
			/// </summary>
			public bool IsDpk { get; set; }

			/// <summary>
			/// Gets whether this is an attached property.
			/// </summary>
			public bool IsAttached { get; }

			/// <summary>
			/// Gets or sets the optional type used to restrict the target type of the attached property.
			/// For instance, <c>System.Windows.Controls.Button</c> can be specified such that the attached property may
			/// only be used on objects that derive from <c>Button</c>.
			/// </summary>
			public ITypeSymbol? AttachmentNarrowingType { get; set; }

			/// <summary>
			/// Gets or sets the type of the dependency property.
			/// </summary>
			public ITypeSymbol? PropertyType { get; set; }

			/// <summary>
			/// Gets or sets the name of the type of the dependency property.
			/// </summary>
			public string PropertyTypeName { get; set; } = "object";
		}

		private static class Diagnostics
		{
			private static readonly DiagnosticDescriptor MismatchedIdentifiersError = new(
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
					MismatchedIdentifiersError,
					fieldSymbol.Locations[0],
					fieldSymbol.Name,
					methodName,
					expectedFieldName,
					initializer);
			}

			private static readonly DiagnosticDescriptor UnexpectedFieldTypeError = new(
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
					UnexpectedFieldTypeError,
					fieldSymbol.Locations[0],
					fieldSymbol.ContainingType.Name,
					fieldSymbol.Name,
					fieldSymbol.Type.ToDisplayString());
			}
		}
	}
}
