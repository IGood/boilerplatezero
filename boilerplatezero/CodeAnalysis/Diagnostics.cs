// Copyright © Ian Good

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Bpz.CodeAnalysis;

public static class Diagnostics
{
	public const string HelpLinkUri = "https://github.com/IGood/boilerplatezero#readme";

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

	private static readonly DiagnosticDescriptor UnexpectedFieldTypeError1 = new(
		id: "BPZ1001",
		title: "Unexpected field type",
		messageFormat: "'{0}' has unexpected type '{1}'. Expected {2}.",
		category: "Types",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: null,
		helpLinkUri: HelpLinkUri,
		customTags: WellKnownDiagnosticTags.Compiler);

	public static Diagnostic UnexpectedFieldType(IFieldSymbol fieldSymbol, params INamedTypeSymbol[] expectedTypeSymbols)
	{
		var displayNames = expectedTypeSymbols.Select(t => $"'{t.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}'");

		string expectedTypes = string.Join(" or ", displayNames);

		return Diagnostic.Create(
			descriptor: UnexpectedFieldTypeError1,
			location: fieldSymbol.Locations[0],
			fieldSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
			fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
			expectedTypes);
	}
}
