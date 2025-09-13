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

    # Create a runner script that will be executed by bazel run
    runner_script_content = [
        "#!/bin/bash",
        "set -euo pipefail",
        "",
        "PROJECT_NAME=\"" + project_name + "\"",
        "PROJECT_DIR=\"" + project_dir + "\"",
        "TARGET_FRAMEWORK=\"" + target_framework + "\"",
        "",
        "# Get the workspace directory where the source files are",
        "WORKSPACE_DIR=\"$${BUILD_WORKSPACE_DIRECTORY:-$$(pwd)}\"",
        "",
        "# Resolve project directory relative to workspace",
        "if [[ \"$$PROJECT_DIR\" == \".\" ]]; then",
        "    FULL_PROJECT_DIR=\"$$WORKSPACE_DIR\"",
        "else",
        "    FULL_PROJECT_DIR=\"$$WORKSPACE_DIR/$$PROJECT_DIR\"",
        "fi",
        "",
        "echo \"Generating $$PROJECT_NAME.csproj in $$FULL_PROJECT_DIR\"",
        "",
        "# Set up runfiles environment for the C# binaries",
        "export RUNFILES_DIR=\"$$0.runfiles\"",
        "export RUNFILES_MANIFEST_FILE=\"$$0.runfiles_manifest\"",
        "",
        "# Run the CompileCsproj tool with the resolved paths",
        "\"$$1\" \"$$PROJECT_NAME\" \"$$FULL_PROJECT_DIR\" \"$$TARGET_FRAMEWORK\" \"$$2\"",
        "",
    ]

    runner_script = "\n".join(runner_script_content)

    # Write the runner script
    native.genrule(
        name = name + "_script",
        outs = [name + "_runner.sh"],
        cmd = "cat > $@ << 'EOF'\n" + runner_script + "\nEOF",
        executable = True,
    )

    # Create the executable target
    native.sh_binary(
        name = name,
        srcs = [":" + name + "_script"],
        args = [
            "$(location @rules_dotnet_csharp_csproj_intellisense//tool:CompileCsproj)",
            "$(location @rules_dotnet_csharp_csproj_intellisense//tool:ListSymbols)",
        ],
        data = [
            "@rules_dotnet_csharp_csproj_intellisense//tool:CompileCsproj",
            "@rules_dotnet_csharp_csproj_intellisense//tool:ListSymbols",
        ],
        visibility = ["//visibility:public"],
    )
