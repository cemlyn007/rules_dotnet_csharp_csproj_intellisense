load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")

# Examples package that includes all example projects
csharp_library(
    name = "library_a",
    srcs = ["LibraryA.cs"],
    target_frameworks = ["net6.0"],
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
    target_frameworks = ["net6.0"],
    visibility = ["//visibility:public"],
    deps = [
        ":library_a",
    ],
)
