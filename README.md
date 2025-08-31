# .NET C# CSProj Generator for Intellisense

A Bazel rule that automatically generates `.csproj` files for C# projects to enable IntelliSense and code completion in VS Code and other IDEs when using Bazel as the build system.

## Overview

When developing C# applications with Bazel, IDEs like VS Code lose IntelliSense capabilities because they rely on `.csproj` files to understand project structure, dependencies, and NuGet packages. This rule solves that problem by:

1. Analyzing your Bazel build graph using `bazel aquery`
2. Extracting C# compilation actions and their dependencies
3. Generating a proper `.csproj` file with all necessary DLL references
4. Enabling full IntelliSense support for your C# codebase

## Features

- **Automatic Dependency Resolution**: Scans Bazel's action graph to find all DLL dependencies
- **NuGet Package Support**: Works with packages managed through Paket/rules_dotnet
- **Multi-Framework Support**: Configurable target framework (defaults to net6.0)
- **Zero Configuration**: Works out of the box with standard Bazel C# projects
- **IDE Integration**: Generated `.csproj` files are compatible with VS Code, Visual Studio, and Rider

## Quick Start

### 1. Add to MODULE.bazel

For Bzlmod (MODULE.bazel):
```starlark
bazel_dep(name = "rules_dotnet_csharp_csproj_intellisense", version = "0.1.0")
git_override(
    module_name = "rules_dotnet_csharp_csproj_intellisense",
    branch = "main",
    remote = "https://github.com/cemlyn007/rules_dotnet_csharp_csproj_intellisense.git",
)
```


### 2. Use in Your BUILD File

```starlark
load("@rules_dotnet_csharp_csproj_intellisense//:generate_csproj.bzl", "generate_csproj")
load("@rules_dotnet//dotnet:defs.bzl", "csharp_binary", "csharp_library")

# Your existing C# targets
csharp_library(
    name = "my_library",
    srcs = ["LibraryA.cs"],
    target_frameworks = ["net6.0"],
    deps = [
        "@paket.main//some.nuget.package",
    ],
)

csharp_binary(
    name = "my_app",
    srcs = ["Main.cs"],
    target_frameworks = ["net6.0"],
    deps = [":my_library"],
)

# Generate .csproj for IntelliSense
generate_csproj(
    name = "intellisense",
    project_name = "MyProject",
    target_framework = "net6.0",
)
```

### 3. Generate the .csproj File

```bash
bazel run :intellisense
```

This will create `MyProject.csproj` in your project directory with all the necessary references for IntelliSense to work.

## How It Works

### Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Bazel Build   │───▶│  compile_csproj │───▶│  Generated      │
│   Graph (aquery)│    │  Python Script  │    │  .csproj File   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Detailed Process

1. **Paket Integration**: Runs `paket2bazel` to ensure NuGet dependencies are available
2. **Build Analysis**: Executes `bazel build //...` to populate the action graph
3. **Action Query**: Uses `bazel aquery //... --output=jsonproto` to extract build actions
4. **C# Action Filtering**: Identifies all `CSharpCompile` actions in the build graph
5. **Dependency Extraction**: Recursively walks dependency trees to find all input DLLs
6. **External DLL Resolution**: Maps Bazel external paths to actual DLL locations
7. **CSProj Generation**: Creates a properly formatted `.csproj` file with all references

### Key Components

#### `generate_csproj.bzl`
The Starlark rule that creates a `py_binary` target to run the CSProj generation.

**Parameters:**
- `name`: Target name for the Bazel rule
- `project_name`: Name of the generated `.csproj` file
- `project_dir`: Directory where the `.csproj` will be created (default: ".")
- `target_framework`: .NET target framework (default: "net6.0")

#### `compile_csproj.py`
The Python script that performs the actual analysis and generation.

## Configuration

### Supported Target Frameworks
- net6.0 (default)
- net7.0
- net8.0
- Any valid .NET target framework moniker

### Example Configurations

#### Basic Usage
```starlark
generate_csproj(
    name = "intellisense",
    project_name = "MyProject",
)
```

## Generated .csproj Structure

The generated `.csproj` file follows this structure:

```xml
<?xml version="1.0" ?>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <!-- Source files would be added here -->
  </ItemGroup>
  <ItemGroup>
    <Reference Include="Google.Protobuf">
      <HintPath>/path/to/external/dll/Google.Protobuf.dll</HintPath>
    </Reference>
    <Reference Include="Grpc.AspNetCore.Server">
      <HintPath>/path/to/external/dll/Grpc.AspNetCore.Server.dll</HintPath>
    </Reference>
    <!-- Additional NuGet package references -->
  </ItemGroup>
</Project>
```

## Requirements

- Bazel with `rules_dotnet`
- Python 3.7+ (for the generation script)
- .NET SDK (for the target framework)
- Paket for NuGet package management (if using external dependencies)

## Limitations

- **Source Files**: Currently does not automatically include source files in the `.csproj` (IntelliSense works with just DLL references)
- **Build Integration**: The `.csproj` is for IntelliSense only; actual builds still use Bazel
- **Manual Regeneration**: Must be re-run when dependencies change

## Troubleshooting

### Common Issues

1. **No DLL References Found**
   - Ensure `bazel build //...` completes successfully
   - Check that your C# targets use `rules_dotnet` properly
   - Verify NuGet dependencies are configured with Paket

2. **IntelliSense Not Working**
   - Make sure the `.csproj` file is in the same directory as your source files
   - Restart VS Code after generating the `.csproj`
   - Check that the target framework matches your project

3. **External DLL Paths Not Found**
   - Run `bazel clean` and regenerate
   - Ensure Paket dependencies are up to date

4. **Duplicate References**
   - Possibly duplicate references in `.csproj` file, at this point you may need to manually edit the file to resolve conflicts and file an issue.
   - See the example `.bazelrc` and how it disables symlinking the `bazel-*` directories, this is because the VS Code C# extension will detect these references as well as the ones defined in the `.csproj`.

### Debugging

To debug the generation process, you can run the Python script directly:

```bash
python compile_csproj.py MyProject . net6.0
```

This will show any errors in the dependency resolution or CSProj generation process.

## Contributing

This rule is designed to be extensible. Key areas for improvement:

- **Source File Inclusion**: Automatically detect and include C# source files
- **Project References**: Support for inter-project references in large codebases
- **Incremental Updates**: Only regenerate when dependencies actually change
- **IDE Configurations**: Support for additional IDE-specific settings

If you have issues, please reach out to me or create an issue!

## Example

See the `example/` directory for a complete working example with:
- A C# library using NuGet packages (Grpc.Net.Client)
- A C# binary consuming the library
- Proper CSProj generation for full IntelliSense support

To try the example:

```bash
cd example
bazel run :intellisense
# Open the directory in VS Code for full IntelliSense support
```