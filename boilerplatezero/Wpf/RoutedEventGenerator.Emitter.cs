// Copyright © Ian Good

using Bpz.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Threading;

namespace Bpz.Wpf;

public partial class RoutedEventGenerator
{
	private void ApppendSource(Compilation compilation, StringBuilder sourceBuilder, GenerationDetails generateThis, CancellationToken cancellationToken)
	{
		string eventName = generateThis.MethodNameNode.Identifier.ValueText;
		string routedEventMemberName = generateThis.FieldSymbol.Name;

		string eventHandlerTypeDoxString = $@"<see cref=""{this.rehTypeSymbol.ToDisplayString()}""/>";

		// Try to get the generic type argument (if it exists, this will be the type of the event handler).
		if (GeneratorOps.TryGetGenericTypeArgument(compilation, generateThis.MethodNameNode, out ITypeSymbol? genTypeArg, cancellationToken))
		{
			// If the type is a multicast delegate, then use it;
			// otherwise, use the type in a `RoutedPropertyChangedEventHandler<>`.
			if (genTypeArg.BaseType?.Equals(this.mdTypeSymbol, SymbolEqualityComparer.Default) ?? false)
			{
				// Good to go! Documentation can reference the generic type parameter.
				eventHandlerTypeDoxString = @"<typeparamref name=""__T""/>";
			}
			else
			{
				// Example: Transform `double` into `RoutedPropertyChangedEventHandler<double>`.
				genTypeArg = this.rpcehTypeSymbol.Construct(genTypeArg);

				// Documentation will appear as something like...
				//	RoutedPropertyChangedEventHandler<T> of double
				string rpcehT = GeneratorOps.ReplaceBrackets(this.rpcehTypeSymbol.ToDisplayString());
				eventHandlerTypeDoxString = $@"<see cref=""{rpcehT}""/> of <typeparamref name=""__T""/>";
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

				if (GeneratorOps.TryGetGenericTypeArgument(compilation, genClassNameNode, out ITypeSymbol? attachmentNarrowingType, cancellationToken))
				{
					generateThis.AttachmentNarrowingType = attachmentNarrowingType;
					targetTypeName = attachmentNarrowingType.ToDisplayString();
					callerExpression = "d";
					moreDox = $@"<br/>
			/// This attached event is only for use with objects of type <typeparamref name=""__TTarget""/>.";
				}
			}
			else
			{
				genClassDecl = "GenAttached";
			}

			// Write the static get/set methods source code.
			string methodsAccess = generateThis.FieldSymbol.DeclaredAccessibility.ToString().ToLower();

			// Something like...
			//	/// <summary>Adds a handler for the <see cref="FooChangedEvent"/> attached event.</summary>
			//	public static void AddFooChangedHandler(DependencyObject d, RoutedPropertyChangedEventHandler<int> handler) => (d as UIElement)?.AddHandler(FooChangedEvent, handler);
			//	/// <summary>Removes a handler for the <see cref="FooChangedEvent"/> attached event.</summary>
			//	public static void RemoveFooChangedHandler(DependencyObject d, RoutedPropertyChangedEventHandler<int> handler) => (d as UIElement)?.RemoveHandler(FooChangedEvent, handler);
			sourceBuilder.Append($@"
		/// <summary>Adds a handler for the <see cref=""{routedEventMemberName}""/> attached event.</summary>
		{methodsAccess} static void Add{eventName}Handler({targetTypeName} d, {generateThis.EventHandlerTypeName} handler) => {callerExpression}.AddHandler({routedEventMemberName}, handler);
		/// <summary>Removes a handler for the <see cref=""{routedEventMemberName}""/> attached event.</summary>
		{methodsAccess} static void Remove{eventName}Handler({targetTypeName} d, {generateThis.EventHandlerTypeName} handler) => {callerExpression}.RemoveHandler({routedEventMemberName}, handler);");
		}
		else
		{
			genClassDecl = "Gen";

			// Let's include the documentation because that's nice.
			// Copy from the field or fall back to a default (so the compiler doesn't warn about missing comments).
			if (GeneratorOps.TryGetDocumentationComment(generateThis.MethodNameNode, out string? doxComment))
			{
				doxComment += "\t\t";
			}
			else
			{
				// Generate useless default documentation like...
				//	/// <summary>Occurs when the <see cref="FooChangedEvent"/> routed event is raised.</summary>
				doxComment = $@"/// <summary>Occurs when the <see cref=""{routedEventMemberName}""/> routed event is raised.</summary>
		";
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
		{doxComment}{eventAccess} event {generateThis.EventHandlerTypeName} {eventName}
		{{
			add => this.AddHandler({routedEventMemberName}, value);
			remove => this.RemoveHandler({routedEventMemberName}, value);
		}}");
		}

		// Write the static helper method.
		string what = generateThis.IsAttached ? "an attached event" : "a routed event";
		string? maybeGeneric = (genTypeArg != null) ? "<__T>" : null;
		string ownerTypeName = GeneratorOps.GetTypeName(generateThis.FieldSymbol.ContainingType);

		sourceBuilder.Append($@"
		private static partial class {genClassDecl}
		{{
			/// <summary>
			/// Registers {what} named ""{eventName}"" whose handler type is {eventHandlerTypeDoxString}.{moreDox}
			/// </summary>
			public static RoutedEvent {eventName}{maybeGeneric}(RoutingStrategy routingStrategy = RoutingStrategy.Direct)
			{{
				return EventManager.RegisterRoutedEvent(""{eventName}"", routingStrategy, typeof({generateThis.EventHandlerTypeName}), typeof({ownerTypeName}));
			}}
		}}
");
	}
}
