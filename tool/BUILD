load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")

csharp_library(
    name = "SymbolAnalysis",
    srcs = ["SymbolAnalysis.cs"],
    target_frameworks = ["net9.0"],
    visibility = ["//visibility:public"],
    deps = [
        "@paket.main//microsoft.codeanalysis.common:microsoft.codeanalysis.common",
        "@paket.main//microsoft.codeanalysis.csharp:microsoft.codeanalysis.csharp",
        "@paket.main//microsoft.codeanalysis:microsoft.codeanalysis",
    ],
)

csharp_binary(
    name = "CompileCsproj",
    srcs = ["CompileCsproj.cs"],
    target_frameworks = ["net9.0"],
    visibility = ["//visibility:public"],
    deps = [
        ":SymbolAnalysis",
    ],
)
