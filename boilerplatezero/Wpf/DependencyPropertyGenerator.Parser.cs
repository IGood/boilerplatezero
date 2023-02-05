// Copyright © Ian Good

using Bpz.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Bpz.Wpf;

public partial class DependencyPropertyGenerator
{
	/// <summary>
	/// Inspects candidates for correctness and updates them with additional information.
	/// Yields those which satisfy requirements for code generation.
	/// </summary>
	private static IEnumerable<GenerationDetails> UpdateAndFilterGenerationRequests(GeneratorExecutionContext context, IEnumerable<GenerationDetails> requests)
	{
		INamedTypeSymbol? dpTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyProperty");
		INamedTypeSymbol? dpkTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Windows.DependencyPropertyKey");
		if (dpTypeSymbol == null || dpkTypeSymbol == null)
		{
			// This probably never happens, but whatevs.
			yield break;
		}

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
					context.ReportDiagnostic(Diagnostics.UnexpectedFieldType(fieldSymbol, dpTypeSymbol, dpkTypeSymbol));
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

		/// <summary>
		/// Gets or sets the name of the method used to validate the property.
		/// </summary>
		public string? ValidationMethodName { get; set; }

		/// <summary>
		/// Gets or sets the name of the method used to coerce the property.
		/// </summary>
		public string? CoercionMethodName { get; set; }

		/// <summary>
		/// Gets or sets the name of the method or event used when the property changes.
		/// </summary>
		public string? ChangedHandlerName { get; set; }

		/// <summary>
		/// Gets a string that represents additional documentation for the property.
		/// </summary>
		public string GetAdditionalDocumentation()
		{
			int numLines = 0;
			string[] lines = new string[4];

			if (this.AttachmentNarrowingType != null)
			{
				lines[numLines++] = $@"<br/>
			/// This attached property is only for use with objects of type <typeparamref name=""__TTarget""/>.";
			}

			if (this.ValidationMethodName != null)
			{
				lines[numLines++] = $@"<br/>
			/// Uses <see cref=""{this.ValidationMethodName}""/> for validation.";
			}

			if (this.CoercionMethodName != null)
			{
				lines[numLines++] = $@"<br/>
			/// Uses <see cref=""{this.CoercionMethodName}""/> for coercion.";
			}

			if (this.ChangedHandlerName != null)
			{
				lines[numLines++] = $@"<br/>
			/// Uses <see cref=""{this.ChangedHandlerName}""/> for changes.";
			}

			return string.Concat(lines);
		}
	}
}
