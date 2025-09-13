#!/bin/bash
set -euo pipefail

PROJECT_NAME="$1"
PROJECT_DIR="$2"
TARGET_FRAMEWORK="$3"
COMPILE_CSPROJ_TOOL="$4"
LIST_SYMBOLS_TOOL="$5"

# Get the workspace directory where the source files are
WORKSPACE_DIR="${BUILD_WORKSPACE_DIRECTORY:-$(pwd)}"

# Resolve project directory relative to workspace
if [[ "$PROJECT_DIR" == "." ]]; then
    FULL_PROJECT_DIR="$WORKSPACE_DIR"
else
    FULL_PROJECT_DIR="$WORKSPACE_DIR/$PROJECT_DIR"
fi

echo "Generating $PROJECT_NAME.csproj in $FULL_PROJECT_DIR"

# Set up runfiles environment for the C# binaries
export RUNFILES_DIR="$0.runfiles"
export RUNFILES_MANIFEST_FILE="$0.runfiles_manifest"

# Run the CompileCsproj tool with the resolved paths
"$COMPILE_CSPROJ_TOOL" "$PROJECT_NAME" "$FULL_PROJECT_DIR" "$TARGET_FRAMEWORK" "$LIST_SYMBOLS_TOOL"
