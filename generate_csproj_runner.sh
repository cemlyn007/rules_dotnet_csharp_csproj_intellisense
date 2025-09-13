#!/bin/bash
set -euo pipefail

PROJECT_NAME="$1"
PROJECT_DIR="$2"
TARGET_FRAMEWORK="$3"
COMPILE_CSPROJ_TOOL="$4"

# Capture any additional targets (arguments 5 and beyond)
shift 4
TARGETS=("$@")

# Get the workspace directory where the source files are
WORKSPACE_DIR="${BUILD_WORKSPACE_DIRECTORY:-$(pwd)}"

# Resolve project directory relative to workspace
if [[ "$PROJECT_DIR" == "." ]]; then
    FULL_PROJECT_DIR="$WORKSPACE_DIR"
else
    FULL_PROJECT_DIR="$WORKSPACE_DIR/$PROJECT_DIR"
fi

if [[ ${#TARGETS[@]} -gt 0 ]]; then
    echo "Generating $PROJECT_NAME.csproj in $FULL_PROJECT_DIR for targets: ${TARGETS[*]}"
else
    echo "Generating $PROJECT_NAME.csproj in $FULL_PROJECT_DIR for all targets (//...)"
fi

# Set up runfiles environment for the C# binaries
export RUNFILES_DIR="$0.runfiles"
export RUNFILES_MANIFEST_FILE="$0.runfiles_manifest"

# Run the CompileCsproj tool with the resolved paths and any additional targets
"$COMPILE_CSPROJ_TOOL" "$PROJECT_NAME" "$FULL_PROJECT_DIR" "$TARGET_FRAMEWORK" "${TARGETS[@]}"
