﻿// Copyright © Ian Good

using Bpz.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bpz.Wpf;

public partial class DependencyPropertyGenerator
{
	private void ApppendSource(Compilation compilation, StringBuilder sourceBuilder, GenerationDetails generateThis, CancellationToken cancellationToken)
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
				sourceBuilder.Append($@"
		[{GeneratorOps.GeneratedCodeAttribute}]
		public static readonly DependencyProperty {dpMemberName} = {dpkMemberName}.DependencyProperty;");
			}
		}

		// Try to get the generic type argument (if it exists, this will be the type of the property).
		GeneratorOps.TryGetGenericTypeArgument(compilation, generateThis.MethodNameNode, out ITypeSymbol? genTypeArg, cancellationToken);

		// We support 0, 1, or 2 arguments. Check for default value and/or flags arguments.
		//	(A) Gen.Foo<T>()
		//	(B) Gen.Foo(defaultValue)
		//	(C) Gen.Foo<T>(flags)
		//	(D) Gen.Foo(defaultValue, flags)
		// The first argument is either the default value or the flags.
		// Note: We do not support properties whose default value is `FrameworkPropertyMetadataOptions` because
		// it's a niche case that would add code complexity.
		ArgumentSyntax? defaultValueArgNode = null;
		ITypeSymbol? typeOfFirstArg = null;
		bool hasFlags = false;
		if (GeneratorOps.TryGetAncestor(generateThis.MethodNameNode, out InvocationExpressionSyntax? invocationExpressionNode))
		{
			var args = invocationExpressionNode.ArgumentList.Arguments;
			if (args.Count > 0)
			{
				// If the first argument is the flags, then we generate (C); otherwise, we generate (B) or (D).
				typeOfFirstArg = GetArgumentType(compilation, args[0], cancellationToken) ?? this.objTypeSymbol;
				if (typeOfFirstArg.Equals(this.flagsTypeSymbol, SymbolEqualityComparer.Default))
				{
					hasFlags = true;
				}
				else
				{
					defaultValueArgNode = args[0];
					hasFlags = args.Count > 1;
				}
			}
		}

		bool hasDefaultValue = defaultValueArgNode != null;

		// Determine the type of the property.
		// If there is a generic type argument, then use that; otherwise, use the type of the default value argument.
		// As a safety precaution - ensure that the generated code is always valid by defaulting to use `object`.
		// But really, if we were unable to get the type, that means the user's code doesn't compile anyhow.
		generateThis.PropertyType =
			genTypeArg
			?? (hasDefaultValue ? typeOfFirstArg : null)
			?? this.objTypeSymbol;

		generateThis.PropertyTypeName = generateThis.PropertyType.ToDisplayString();

		string genClassDecl;

		if (generateThis.IsAttached)
		{
			string targetTypeName = "DependencyObject";

			if (generateThis.MethodNameNode.Parent is MemberAccessExpressionSyntax memberAccessExpr &&
				memberAccessExpr.Expression is GenericNameSyntax genClassNameNode)
			{
				genClassDecl = "GenAttached<__TTarget> where __TTarget : DependencyObject";

				if (GeneratorOps.TryGetGenericTypeArgument(compilation, genClassNameNode, out ITypeSymbol? attachmentNarrowingType, cancellationToken))
				{
					generateThis.AttachmentNarrowingType = attachmentNarrowingType;
					targetTypeName = attachmentNarrowingType.ToDisplayString();
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
			//	/// <summary>Gets the value of the <see cref="FooProperty"/> attached property.</summary>
			//	public static int GetFoo(DependencyObject d) => (int)d.GetValue(FooProperty);
			//	/// <summary>Sets the value of the <see cref="FooProperty"/> attached property.</summary>
			//	private static void SetFoo(DependencyObject d, int value) => d.SetValue(FooPropertyKey);
			sourceBuilder.Append($@"
		/// <summary>Gets the value of the <see cref=""{dpMemberName}""/> attached property.</summary>
		[{GeneratorOps.GeneratedCodeAttribute}]
		{getterAccess} static {generateThis.PropertyTypeName} Get{propertyName}({targetTypeName} d) => ({generateThis.PropertyTypeName})d.GetValue({dpMemberName});
		/// <summary>Sets the value of the <see cref=""{dpMemberName}""/> attached property.</summary>
		[{GeneratorOps.GeneratedCodeAttribute}]
		{setterAccess} static void Set{propertyName}({targetTypeName} d, {generateThis.PropertyTypeName} value) => d.SetValue({setterArg0}, value);");
		}
		else
		{
			genClassDecl = "Gen";

			// Write the instance property source code.
			string propertyAccess = dpAccess.ToString().ToLower();
			string? setterAccess = generateThis.IsDpk ? (dpkAccess.ToString().ToLower() + " ") : null;
			string setterArg0 = generateThis.IsDpk ? dpkMemberName : dpMemberName;

			// Let's include documentation because that's nice.
			// Copy from the field or fall back to a default (so the compiler doesn't warn about missing comments).
			if (GeneratorOps.TryGetDocumentationComment(generateThis.MethodNameNode, out string? doxComment))
			{
				doxComment += "\t\t";
			}
			else
			{
				// Generate useless default documentation like...
				//	/// <summary>Gets or sets the value of the <see cref="FooProperty"/> dependency property.</summary>
				string? orSets = (setterAccess == null) ? "or sets " : null;
				doxComment = $@"/// <summary>Gets {orSets}the value of the <see cref=""{dpMemberName}""/> dependency property.</summary>
		";
			}

			// Something like...
			//	public int Foo
			//	{
			//		get => (int)this.GetValue(FooProperty);
			//		private set => this.SetValue(FooPropertyKey, value);
			//	}
			sourceBuilder.Append($@"
		{doxComment}[{GeneratorOps.GeneratedCodeAttribute}]
		{propertyAccess} {generateThis.PropertyTypeName} {propertyName}
		{{
			get => ({generateThis.PropertyTypeName})this.GetValue({dpMemberName});
			{setterAccess}set => this.SetValue({setterArg0}, value);
		}}");
		}

		// Write the static helper method.
		string what = generateThis.IsDpk
			? (generateThis.IsAttached ? "a read-only attached property" : "a read-only dependency property")
			: (generateThis.IsAttached ? "an attached property" : "a dependency property");

		string returnType = generateThis.FieldSymbol.Type.Name;

		string parameters;
		{
			int numParams = 0;
			string[] @params = new string[2];

			if (hasDefaultValue)
			{
				@params[numParams++] = "__T defaultValue";
			}

			if (hasFlags)
			{
				@params[numParams++] = "FrameworkPropertyMetadataOptions flags";
			}

			parameters = string.Join(", ", @params, 0, numParams);
		}

		string a = generateThis.IsAttached ? "Attached" : "";
		string ro = generateThis.IsDpk ? "ReadOnly" : "";
		string ownerTypeName = GeneratorOps.GetTypeName(generateThis.FieldSymbol.ContainingType);
		string metadataStr = this.GetPropertyMetadataInstance(generateThis, hasDefaultValue, hasFlags, out string validationCallbackStr);

		string moreDox = generateThis.GetAdditionalDocumentation();

		sourceBuilder.Append($@"
		private static partial class {genClassDecl}
		{{
			/// <summary>
			/// Registers {what} named ""{propertyName}"" whose type is <typeparamref name=""__T""/>.{moreDox}
			/// </summary>
			[{GeneratorOps.GeneratedCodeAttribute}]
			public static {returnType} {propertyName}<__T>({parameters})
			{{
				var metadata = {metadataStr};
				return DependencyProperty.Register{a}{ro}(""{propertyName}"", typeof(__T), typeof({ownerTypeName}), metadata, {validationCallbackStr});
			}}
		}}
");
	}

	/// <summary>
	/// Gets source text that creates the property metadata object and validation callback.
	/// Accounts for whether a default value exists.
	/// Accounts for whether a compatible property-changed handler exists.
	/// Accounts for whether a compatible coercion handler exists.
	/// Accounts for whether a compatible validation handler exists.
	/// </summary>
	private string GetPropertyMetadataInstance(GenerationDetails generateThis, bool hasDefaultValue, bool hasFlags, out string validationCallbackStr)
	{
		INamedTypeSymbol ownerType = generateThis.FieldSymbol.ContainingType;
		string propertyName = generateThis.MethodNameNode.Identifier.ValueText;
		string coerceMethodName = "Coerce" + propertyName;
		string validateMethodName = "IsValid" + propertyName;

		AssociatedHandlers foundAssociates = AssociatedHandlers.None;
		ChangeHandlerKind changeHandlerKind = ChangeHandlerKind.None;
		string changeHandler = "null";
		string coercionHandler = "null";
		string validationHandler = "null";

		// Look for associated handlers...
		foreach (ISymbol memberSymbol in ownerType.GetMembers())
		{
			string maybeChangeHandler;

			switch (memberSymbol.Kind)
			{
				case SymbolKind.Field:
					// If we haven't found a routed event or better, then check this field.
					if (changeHandlerKind < ChangeHandlerKind.RoutedEvent &&
						_TryGetChangeHandler2((IFieldSymbol)memberSymbol, out maybeChangeHandler))
					{
						changeHandlerKind = ChangeHandlerKind.RoutedEvent;
						changeHandler = maybeChangeHandler;
					}
					break;

				case SymbolKind.Method:
					// If we haven't found a static property-changed method, then check this method.
					if (changeHandlerKind < ChangeHandlerKind.StaticMethod &&
						_TryGetChangeHandler((IMethodSymbol)memberSymbol, out maybeChangeHandler, out bool isStatic))
					{
						if (isStatic)
						{
							foundAssociates |= AssociatedHandlers.PropertyChanged;
							changeHandlerKind = ChangeHandlerKind.StaticMethod;
						}
						else
						{
							changeHandlerKind = ChangeHandlerKind.InstanceMethod;
						}

						changeHandler = maybeChangeHandler;

						break;
					}

					// If we haven't found a coercion handler, then check this method.
					if (!foundAssociates.HasFlag(AssociatedHandlers.Coerce) &&
						_TryGetCoercionHandler((IMethodSymbol)memberSymbol, out coercionHandler))
					{
						foundAssociates |= AssociatedHandlers.Coerce;
					}

					// If we haven't found a validation handler, then check this method.
					if (!foundAssociates.HasFlag(AssociatedHandlers.Validate) &&
						_TryGetValidationHandler((IMethodSymbol)memberSymbol, out validationHandler))
					{
						foundAssociates |= AssociatedHandlers.Validate;
					}
					break;

				default:
					continue;
			}

			if (foundAssociates == AssociatedHandlers.All)
			{
				break;
			}
		}

		// See if we have any routed events like...
		//	RoutedEvent FooChangedEvent = Gen.FooChanged<int>();
		bool _TryGetChangeHandler2(IFieldSymbol fieldSymbol, out string changeHandler)
		{
			string fieldName = fieldSymbol.Name;
			if (fieldSymbol.IsStatic &&
				fieldSymbol.IsReadOnly &&
				fieldName == propertyName + "ChangedEvent" &&
				fieldSymbol.Type.Equals(this.reTypeSymbol, SymbolEqualityComparer.Default))
			{
				string? maybeCastArgs = (generateThis.PropertyType?.SpecialType == SpecialType.System_Object)
					? null
					: $"({generateThis.PropertyTypeName})";

				// Something like...
				//	static (d, e) => ((UIElement)d).RaiseEvent(new RoutedPropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue, FooChangedEvent))
				changeHandler = $"static (d, e) => ((UIElement)d).RaiseEvent(new RoutedPropertyChangedEventArgs<{generateThis.PropertyTypeName}>({maybeCastArgs}e.OldValue, {maybeCastArgs}e.NewValue, {fieldName}))";
				generateThis.ChangedHandlerName = fieldName;
				return true;
			}

			changeHandler = "null";
			return false;
		}

		// See if we have any property-changed handlers like...
		//	static void FooPropertyChanged(Widget self, DependencyPropertyChangedEventArgs e) { ... }
		//	static void OnFooChanged(Widget self, DependencyPropertyChangedEventArgs e) { ... }
		//	void FooChanged(DependencyPropertyChangedEventArgs e) { ... }
		//	void OnFooChanged(string oldFoo, string newFoo) { ... }
		bool _TryGetChangeHandler(IMethodSymbol methodSymbol, out string changeHandler, out bool isStatic)
		{
			isStatic = methodSymbol.IsStatic;

			if (methodSymbol.ReturnsVoid)
			{
				string methodName = methodSymbol.Name;

				if (isStatic)
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
								generateThis.ChangedHandlerName = changeHandler;
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
								//	static (d, e) => FooPropertyChanged((Goodies.Widget)d, e)
								changeHandler = $"static (d, e) => {methodName}(({p0TypeSymbol.ToDisplayString()})d, e)";
								generateThis.ChangedHandlerName = methodName;
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
							//	static (d, e) => ((Goodies.Widget)d).OnFooChanged((int)e.OldValue, (int)e.NewValue)
							changeHandler = $"static (d, e) => (({ownerType.ToDisplayString()})d).{methodName}({maybeCastArgs}e.OldValue, {maybeCastArgs}e.NewValue)";
							generateThis.ChangedHandlerName = methodName;
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
							//	static (d, e) => ((Goodies.Widget)d).OnFooChanged(e)
							changeHandler = $"static (d, e) => (({ownerType.ToDisplayString()})d).{methodName}(e)";
							generateThis.ChangedHandlerName = methodName;
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
					//	static (d, baseValue) => CoerceFoo((Goodies.Widget)d, (int)baseValue)
					coercionHandler = $"static (d, baseValue) => {methodName}({maybeCastArg0}d, {maybeCastArg1}baseValue)";
				}
				else
				{
					// Signature is compatible with `System.Windows.CoerceValueCallback`, so we can just use the method name.
					coercionHandler = methodName;
				}

				generateThis.CoercionMethodName = methodName;

				return true;
			}

			coercionHandler = "null";
			return false;
		}

		// See if we have any validation handlers like...
		//	static bool IsValidFoo(object value) { ... }
		//	static bool IsValidFoo(int value) { ... }
		bool _TryGetValidationHandler(IMethodSymbol methodSymbol, out string validationHandler)
		{
			string methodName = methodSymbol.Name;
			if (methodSymbol.IsStatic &&
				methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean &&
				methodSymbol.Parameters.Length == 1 &&
				methodName == validateMethodName)
			{
				// Ensure type of p0 is valid. Must be `object` or the property type.
				ITypeSymbol p0TypeSymbol = methodSymbol.Parameters[0].Type;
				if (p0TypeSymbol.SpecialType != SpecialType.System_Object)
				{
					if (!p0TypeSymbol.Equals(generateThis.PropertyType, SymbolEqualityComparer.Default))
					{
						validationHandler = "null";
						return false;
					}

					// Something like...
					//	value => IsValidFoo((int)value)
					validationHandler = $"value => {methodName}(({generateThis.PropertyTypeName})value)";
				}
				else
				{
					// Signature is compatible with `System.Windows.ValidateValueCallback`, so we can just use the method name.
					validationHandler = methodName;
				}

				generateThis.ValidationMethodName = methodName;

				return true;
			}

			validationHandler = "null";
			return false;
		}

		validationCallbackStr = validationHandler;

		if (hasFlags)
		{
			string defaultValue = hasDefaultValue ? "defaultValue" : "default(__T)";
			return $"new FrameworkPropertyMetadata({defaultValue}, flags, {changeHandler}, {coercionHandler})";
		}

		if (hasDefaultValue)
		{
			return $"new PropertyMetadata(defaultValue, {changeHandler}, {coercionHandler})";
		}

		if (changeHandler != "null")
		{
			return $"new PropertyMetadata({changeHandler}) {{ CoerceValueCallback = {coercionHandler} }}";
		}

		if (coercionHandler != "null")
		{
			return $"new PropertyMetadata() {{ CoerceValueCallback = {coercionHandler} }}";
		}

		return $"(PropertyMetadata){nullLiteral}";
	}

	/// <summary>
	/// Attempts to gets the type of an argument node.
	/// </summary>
	private static ITypeSymbol? GetArgumentType(Compilation compilation, ArgumentSyntax argumentNode, CancellationToken cancellationToken)
	{
		var model = compilation.GetSemanticModel(argumentNode.SyntaxTree);
		var typeInfo = model.GetTypeInfo(argumentNode.Expression, cancellationToken);
		ITypeSymbol? argType = typeInfo.Type;

		// Handle expressions like `(string?)null`.
		// A nullable ref type like `string?` loses its annotation here. Let's put it back.
		// Note: Nullable value types like `int?` do not have this issue.
		if (argType != null &&
			argType.IsReferenceType &&
			argumentNode.Expression is CastExpressionSyntax castNode &&
			castNode.Type is NullableTypeSyntax)
		{
			argType = argType.WithNullableAnnotation(NullableAnnotation.Annotated);
		}

		return argType;
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
	/// Specifies potential handler behaviors that are associated with a dependency property.
	/// </summary>
	[Flags]
	private enum AssociatedHandlers
	{
		None = 0,
		PropertyChanged = 1 << 0,
		Coerce = 1 << 1,
		Validate = 1 << 2,
		All = PropertyChanged | Coerce | Validate,
	}

	/// <summary>
	/// Specifies the possible kinds of change-handlers.
	/// Multiple candidates may be found when looking for associated handlers.
	/// Higher values have higher priority.
	/// </summary>
	private enum ChangeHandlerKind
	{
		None,
		RoutedEvent,
		InstanceMethod,
		StaticMethod,
	}
}
