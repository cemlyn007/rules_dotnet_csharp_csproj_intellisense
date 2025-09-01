load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary")

csharp_binary(
    name = "ListSymbols",
    srcs = [
        "ListSymbols.cs",
    ],
    target_frameworks = ["net9.0"],
    visibility = ["//visibility:public"],
    deps = [
        "@paket.main//microsoft.codeanalysis.common:microsoft.codeanalysis.common",
        "@paket.main//microsoft.codeanalysis.csharp:microsoft.codeanalysis.csharp",
        "@paket.main//microsoft.codeanalysis:microsoft.codeanalysis",
    ],
)
