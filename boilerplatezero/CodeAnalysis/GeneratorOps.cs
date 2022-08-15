// Copyright © Ian Good

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Bpz.CodeAnalysis
{
	public static class GeneratorOps
	{
		static GeneratorOps()
		{
			string tool = typeof(GeneratorOps).Assembly.GetName().Name;
			System.Version version = typeof(GeneratorOps).Assembly.GetName().Version;
			GeneratedCodeAttribute = $"global::System.CodeDom.Compiler.GeneratedCode(\"{tool}\", \"{version}\")";
		}

		public static readonly string GeneratedCodeAttribute;

		/// <summary>
		/// Attempts to get the generic type argument of a node.<br/>
		/// Example: <c>Foo&lt;string?&gt;</c> returns <c>true</c> with <c>string?</c>.
		/// </summary>
		public static bool TryGetGenericTypeArgument(Compilation compilation, SyntaxNode node, [NotNullWhen(true)] out ITypeSymbol? typeArgument, CancellationToken cancellationToken)
		{
			if (node is GenericNameSyntax genNameNode)
			{
				var typeArgNode = genNameNode.TypeArgumentList.Arguments.FirstOrDefault();
				if (typeArgNode != null)
				{
					var model = compilation.GetSemanticModel(typeArgNode.SyntaxTree);
					var typeInfo = model.GetTypeInfo(typeArgNode, cancellationToken);
					typeArgument = typeInfo.Type;

					// A nullable ref type like `string?` loses its annotation here. Let's put it back.
					// Note: Nullable value types like `int?` do not have this issue.
					if (typeArgument != null &&
						typeArgument.IsReferenceType &&
						typeArgNode is NullableTypeSyntax)
					{
						typeArgument = typeArgument.WithNullableAnnotation(NullableAnnotation.Annotated);
					}

					return typeArgument != null;
				}
			}

			typeArgument = null;
			return false;
		}

		/// <summary>
		/// Traverses the syntax tree upward from <paramref name="syntaxNode"/> and returns the first node of type
		/// <typeparamref name="T"/> if one exists.
		/// </summary>
		public static bool TryGetAncestor<T>(SyntaxNode syntaxNode, [NotNullWhen(true)] out T? ancestorSyntaxNode) where T : SyntaxNode
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
		/// Attempts to get the documentation comment associated with the field syntax node ancestor if one exists.
		/// </summary>
		public static bool TryGetDocumentationComment(SyntaxNode syntaxNode, [NotNullWhen(true)] out string? documentationComment)
		{
			if (TryGetAncestor(syntaxNode, out FieldDeclarationSyntax? fieldDeclNode))
			{
				documentationComment = fieldDeclNode
					.DescendantTrivia()
					.FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
					.ToFullString();
				if (documentationComment.Length != 0)
				{
					return true;
				}
			}

			documentationComment = null;
			return false;
		}

		/// <summary>
		/// Gets the name of the type including generic type parameters (ex: "Widget&lt;TSomething&gt;").
		/// </summary>
		public static string GetTypeName(INamedTypeSymbol typeSymbol)
		{
			// If the type isn't generic, then we can take a fast path.
			return typeSymbol.IsGenericType
				? typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
				: typeSymbol.Name;
		}

		/// <summary>
		/// Replaces angle brackets ("&lt;&gt;") with curly braces ("{}").
		/// </summary>
		public static string ReplaceBrackets(string typeName)
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
				if (c == '<')
				{
					c = '{';
				}
				else if (c == '>')
				{
					c = '}';
				}
			}

			return new string(chars);
		}
	}
}
