"""Rule for generating .csproj files for C# IntelliSense support."""

def generate_csproj(
        name,
        project_name,
        project_dir = ".",
        target_framework = "net9.0"):
    """Generates a .csproj file for C# intellisense support.

    This creates an executable target that can be run with `bazel run` to generate
    the .csproj file in the source tree.

    Args:
        name: The name of the target.
        project_name: Name of the C# project.
        project_dir: Directory of the project (default: ".").
        target_framework: Target .NET framework (default: "net9.0").
    """

    # Create the executable target using the external shell script
    native.sh_binary(
        name = name,
        srcs = ["@rules_dotnet_csharp_csproj_intellisense//:generate_csproj_runner.sh"],
        args = [
            project_name,
            project_dir,
            target_framework,
            "$(location @rules_dotnet_csharp_csproj_intellisense//tool:CompileCsproj)",
        ],
        data = [
            "@rules_dotnet_csharp_csproj_intellisense//tool:CompileCsproj",
        ],
        visibility = ["//visibility:public"],
    )
