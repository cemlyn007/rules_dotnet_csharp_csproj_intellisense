using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SymbolAnalysis
{
    public static class SymbolLister
    {
        /// <summary>
        /// Analyzes a DLL or C# source file and returns a formatted string containing all public symbols.
        /// </summary>
        /// <param name="inputPath">Path to the .dll or .cs file to analyze</param>
        /// <returns>A formatted string containing all public symbols, or null if the file cannot be processed</returns>
        public static string? AnalyzeFile(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                return null;
            }

            if (!System.IO.File.Exists(inputPath))
            {
                return null;
            }

            CSharpCompilation compilation;
            if (inputPath.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
            {
                // Load the DLL as a metadata reference
                var metadataReference = MetadataReference.CreateFromFile(inputPath);
                compilation = CSharpCompilation.Create("TempCompilation")
                    .WithReferences(metadataReference);
            }
            else if (inputPath.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
            {
                // Parse the C# source file
                var sourceText = System.IO.File.ReadAllText(inputPath);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: inputPath);
                compilation = CSharpCompilation.Create("TempCompilation")
                    .AddSyntaxTrees(syntaxTree);
            }
            else
            {
                return null;
            }

            // Get the global namespace
            var globalNamespace = compilation.GlobalNamespace;

            // Recursively list all symbols
            return ListSymbols(globalNamespace, indent: 0);
        }

        /// <summary>
        /// Analyzes a compilation and returns a formatted string containing all public symbols.
        /// </summary>
        /// <param name="compilation">The compilation to analyze</param>
        /// <returns>A formatted string containing all public symbols</returns>
        public static string AnalyzeCompilation(CSharpCompilation compilation)
        {
            var globalNamespace = compilation.GlobalNamespace;
            return ListSymbols(globalNamespace, indent: 0);
        }

        /// <summary>
        /// Recursively lists all symbols in a namespace or type and returns a formatted string.
        /// </summary>
        /// <param name="symbol">The namespace or type symbol to analyze</param>
        /// <param name="indent">The indentation level for formatting</param>
        /// <returns>A formatted string containing the symbol information</returns>
        private static string ListSymbols(INamespaceOrTypeSymbol symbol, int indent)
        {
            var result = new System.Text.StringBuilder();

            // List public method signatures
            var format = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue |
                    SymbolDisplayMemberOptions.IncludeRef,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeModifiers,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                localOptions:
                    SymbolDisplayLocalOptions.IncludeType |
                    SymbolDisplayLocalOptions.IncludeConstantValue |
                    SymbolDisplayLocalOptions.IncludeRef,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword |
                    SymbolDisplayKindOptions.IncludeNamespaceKeyword |
                    SymbolDisplayKindOptions.IncludeTypeKeyword
            );

            // Append the current symbol with indentation
            result.AppendLine(new string(' ', indent * 2) + symbol.ToDisplayString(format));

            // Get members (types or namespace members)
            foreach (var member in symbol.GetMembers().OrderBy(m => m.Name))
            {
                // Handle namespaces
                if (member is INamespaceSymbol namespaceSymbol)
                {
                    result.Append(ListSymbols(namespaceSymbol, indent + 1));
                }
                // Handle types (classes, interfaces, structs, etc.)
                else if (member is INamedTypeSymbol typeSymbol)
                {
                    // Optionally filter by accessibility (e.g., public only)
                    if (typeSymbol.DeclaredAccessibility == Accessibility.Public)
                    {
                        result.Append(ListSymbols(typeSymbol, indent + 1));

                        foreach (var typeMember in typeSymbol.GetMembers().OfType<IMethodSymbol>().OrderBy(m => m.Name))
                        {
                            if (typeMember.DeclaredAccessibility == Accessibility.Public && !typeMember.MethodKind.ToString().Contains("Constructor"))
                            {
                                var signature = typeMember.ToDisplayString(format);
                                result.AppendLine(new string(' ', (indent + 2) * 2) + signature);
                            }
                        }
                    }
                }
            }

            return result.ToString();
        }
    }
}
