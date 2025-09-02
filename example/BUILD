load("@protobuf//bazel:proto_library.bzl", "proto_library")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")
load("@rules_dotnet_csharp_csproj_intellisense//:generate_csproj.bzl", "generate_csproj")
load("@rules_proto_grpc_csharp//:defs.bzl", "csharp_proto_library")

proto_library(
    name = "thing_proto",
    srcs = ["thing.proto"],
    import_prefix = "example/proto",
    visibility = ["//visibility:public"],
    deps = [
        "@protobuf//:any_proto",
    ],
)

csharp_proto_library(
    name = "thing_csharp_proto",
    protos = [
        ":thing_proto",
    ],
)

csharp_library(
    name = "library_a",
    srcs = ["LibraryA.cs"],
    target_frameworks = ["net9.0"],
    visibility = ["//visibility:public"],
)

csharp_binary(
    name = "main",
    srcs = [
        "Main.cs",
    ],
    target_frameworks = ["net9.0"],
    visibility = ["//visibility:public"],
    deps = [
        ":library_a",
        ":thing_csharp_proto",
    ],
)

generate_csproj(
    name = "intellisense",
    project_name = "Example",
    target_framework = "net9.0",
)
