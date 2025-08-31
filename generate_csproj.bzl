def generate_csproj(
        name,
        project_name,
        project_dir = ".",
        target_framework = "net6.0"):
    """Generates a .csproj file for C# intellisense support.

    Args:
        name: The name of the target.
        project_name: Name of the C# project.
        project_dir: Directory of the project (default: ".").
        target_framework: Target .NET framework (default: "net6.0").
    """

    # Create py_binary that directly runs compile_csproj.py
    native.py_binary(
        name = name,
        main = "@dotnet_csharp_csproj_intellisense//:compile_csproj.py",
        srcs = ["@dotnet_csharp_csproj_intellisense//:compile_csproj.py"],
        args = [project_name, project_dir, target_framework],
        imports = [""],
        visibility = ["//visibility:public"],
    )
