// Copyright © Ian Good

using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Bpz.CodeAnalysis
{
	public static class GeneratorOps
	{
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
		/// Gets the name of the type including generic type parameters (ex: "Widget&lt;TSomething&gt;").
		/// </summary>
		public static string GetTypeName(INamedTypeSymbol typeSymbol)
		{
			// If the type isn't generic, then we can take a fast path.
			return typeSymbol.IsGenericType ? typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) : typeSymbol.Name;
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
