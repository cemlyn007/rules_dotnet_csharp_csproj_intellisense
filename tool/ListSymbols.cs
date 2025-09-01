using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length == 0)
{
    Console.WriteLine("Usage: ListSymbols <path-to-dll-or-cs-file>");
    return;
}

var inputPath = args[0];
if (!System.IO.File.Exists(inputPath))
{
    Console.WriteLine($"File not found: {inputPath}");
    return;
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
    Console.WriteLine("Unsupported file type. Please provide a .dll or .cs file.");
    return;
}

// Get the global namespace
var globalNamespace = compilation.GlobalNamespace;

// Recursively list all symbols
ListSymbols(globalNamespace, indent: 0);

void ListSymbols(INamespaceOrTypeSymbol symbol, int indent)
{
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
    // Print the current symbol with indentation
    Console.WriteLine(new string(' ', indent * 2) + symbol.ToDisplayString(format));

    // Get members (types or namespace members)
    foreach (var member in symbol.GetMembers().OrderBy(m => m.Name))
    {
        // Handle namespaces
        if (member is INamespaceSymbol namespaceSymbol)
        {
            ListSymbols(namespaceSymbol, indent + 1);
        }
        // Handle types (classes, interfaces, structs, etc.)
        else if (member is INamedTypeSymbol typeSymbol)
        {
            // Optionally filter by accessibility (e.g., public only)
            if (typeSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                ListSymbols(typeSymbol, indent + 1);

                foreach (var typeMember in typeSymbol.GetMembers().OfType<IMethodSymbol>().OrderBy(m => m.Name))
                {
                    if (typeMember.DeclaredAccessibility == Accessibility.Public && !typeMember.MethodKind.ToString().Contains("Constructor"))
                    {
                        var signature = typeMember.ToDisplayString(format);
                        Console.WriteLine(new string(' ', (indent + 2) * 2) + signature);
                    }
                }
            }
        }
    }
}
