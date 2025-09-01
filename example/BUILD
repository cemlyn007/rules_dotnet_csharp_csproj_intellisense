load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")
load("@rules_dotnet_csharp_csproj_intellisense//:generate_csproj.bzl", "generate_csproj")

# Examples package that includes all example projects
csharp_library(
    name = "library_a",
    srcs = ["LibraryA.cs"],
    target_frameworks = ["net8.0"],
    visibility = ["//visibility:public"],
    deps = [
        "@paket.main//google.protobuf",
        "@paket.main//grpc.aspnetcore.server",
        "@paket.main//grpc.core.api",
        "@paket.main//grpc.net.client",
        "@paket.main//grpc.net.common:grpc.net.common",
        "@paket.main//microsoft.extensions.logging.abstractions:microsoft.extensions.logging.abstractions",
    ],
)

csharp_binary(
    name = "main",
    srcs = ["Main.cs"],
    target_frameworks = ["net8.0"],
    visibility = ["//visibility:public"],
    deps = [
        ":library_a",
    ],
)

generate_csproj(
    name = "intellisense",
    project_name = "Project",
    target_framework = "net8.0",
)
