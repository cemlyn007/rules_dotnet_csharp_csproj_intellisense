import argparse
import dataclasses
import json
import os
import subprocess
import xml.etree.ElementTree as ET
from collections.abc import Iterable
from pathlib import Path
from typing import Any
from xml.dom import minidom

BAZEL_BINARY = "bazel"


def _run_paket2bazel(directory: Path):
    subprocess.run(
        [
            BAZEL_BINARY,
            "run",
            "@rules_dotnet//tools/paket2bazel",
            "--",
            "--dependencies-file",
            str(directory / "paket.dependencies"),
            "--output-folder",
            str(directory),
        ],
        capture_output=True,
        text=True,
        cwd=directory,
    )


def _run_bazel_build(directory: Path):
    subprocess.run(
        [
            BAZEL_BINARY,
            "build",
            "//...",
        ],
        capture_output=True,
        text=True,
        cwd=directory,
    )


def _run_bazel_aquery(directory: Path):
    process_result = subprocess.run(
        [BAZEL_BINARY, "aquery", "//...", "--output=jsonproto"],
        check=True,
        capture_output=True,
        cwd=directory,
    )
    aquery = json.loads(process_result.stdout)
    return aquery


def _run_bazel_output_base(directory: Path):
    process_result = subprocess.run(
        [BAZEL_BINARY, "info", "output_base"],
        check=True,
        capture_output=True,
        cwd=directory,
        text=True,
    )
    output_base = process_result.stdout.strip()
    return Path(output_base)


@dataclasses.dataclass
class _Action:
    target_id: int
    inputs: list[Path]
    outputs: list[Path]
    primary_output: Path


def _get_path(path_fragments: list[dict], path_fragment_id: int):
    fragment = (fragment,) = [
        other_fragment
        for other_fragment in path_fragments
        if other_fragment["id"] == path_fragment_id
    ]
    related_fragments: list[str] = [fragment["label"]]
    while "parentId" in fragment:
        (fragment,) = [
            other_fragment
            for other_fragment in path_fragments
            if other_fragment["id"] == fragment["parentId"]
        ]
        related_fragments.append(fragment["label"])

    path = Path(related_fragments.pop())
    while related_fragments:
        path = path / related_fragments.pop()
    return path


def _select(iterable: Iterable[dict[str, Any]], key: str, value: int) -> dict[str, Any]:
    (item,) = [item for item in iterable if item[key] == value]
    return item


class _ActionsFactory:
    def __init__(self, aquery: dict[str, Any]):
        self.aquery = aquery

    def __call__(self) -> list[_Action]:
        actions: list[_Action] = []
        for aquery_action in self.aquery["actions"]:
            if aquery_action["mnemonic"] == "CSharpCompile":
                action = self._get_action(aquery_action["targetId"])
                actions.append(action)
        return actions

    def _get_artifact_path(self, artifact_id: int):
        artifact = _select(self.aquery["artifacts"], "id", artifact_id)
        path_fragment_id = artifact["pathFragmentId"]
        path = _get_path(self.aquery["pathFragments"], path_fragment_id)
        return path

    def _recursively_get_inputs(self, dep_set_id: int) -> list[Path]:
        inputs = []
        dep_set_of_files = _select(self.aquery["depSetOfFiles"], "id", dep_set_id)
        direct_artifact_ids = dep_set_of_files["directArtifactIds"]
        for direct_artifact_id in direct_artifact_ids:
            path = self._get_artifact_path(direct_artifact_id)
            inputs.append(path)
        for transitive_dep_set_id in dep_set_of_files.get("transitiveDepSetIds", []):
            inputs.extend(self._recursively_get_inputs(transitive_dep_set_id))
        return inputs

    def _get_action(self, target_id: int) -> _Action:
        inputs: list[Path] = []
        outputs: list[Path] = []
        (action,) = [
            action
            for action in self.aquery["actions"]
            # A targetId can correspond to multiple actions:
            if action["targetId"] == target_id and action["mnemonic"] == "CSharpCompile"
        ]
        input_dep_set_ids = action["inputDepSetIds"]
        for input_dep_set_id in input_dep_set_ids:
            dep_set_of_files = _select(
                self.aquery["depSetOfFiles"], "id", input_dep_set_id
            )

            direct_artifact_ids = dep_set_of_files["directArtifactIds"]
            for direct_artifact_id in direct_artifact_ids:
                path = self._get_artifact_path(direct_artifact_id)
                inputs.append(path)

            transitive_dep_set_ids = dep_set_of_files.get("transitiveDepSetIds", [])
            for transitive_dep_set_id in transitive_dep_set_ids:
                paths = self._recursively_get_inputs(transitive_dep_set_id)
                inputs.extend(paths)

        output_ids = action["outputIds"]
        for output_id in output_ids:
            path = self._get_artifact_path(output_id)
            outputs.append(path)

        primary_output_id = action["primaryOutputId"]
        primary_output_artifact = _select(
            self.aquery["artifacts"], "id", primary_output_id
        )
        primary_output_path_fragment_id = primary_output_artifact["pathFragmentId"]
        primary_output_path = _get_path(
            self.aquery["pathFragments"], primary_output_path_fragment_id
        )

        (dotnet_cli_home,) = [
            env_var["value"]
            for env_var in action["environmentVariables"]
            if env_var["key"] == "DOTNET_CLI_HOME"
        ]
        dotnet_cli_home_path = Path(dotnet_cli_home)
        inputs = [
            input_path
            for input_path in inputs
            if not input_path.is_relative_to(dotnet_cli_home_path)
        ]
        return _Action(
            target_id=target_id,
            inputs=inputs,
            outputs=outputs,
            primary_output=primary_output_path,
        )


def _get_actions(directory: Path) -> list[_Action]:
    _run_paket2bazel(directory)
    _run_bazel_build(directory)
    aquery = _run_bazel_aquery(directory)
    actions_factory = _ActionsFactory(aquery)
    return actions_factory()


def _find_external_path(external: Path, path: Path) -> Path | None:
    full_path = external.parent / path
    if full_path.exists():
        return full_path
    raise FileNotFoundError(f"External path not found: {full_path}")


def _generate_csproj(
    csproj_path: Path,
    source_files: list[str],
    dll_references: list[str],
    target_framework: str,
) -> None:
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk")
    prop_group = ET.SubElement(project, "PropertyGroup")
    tf = ET.SubElement(prop_group, "TargetFramework")
    tf.text = target_framework

    item_group_sources = ET.SubElement(project, "ItemGroup")
    for src in source_files:
        ET.SubElement(item_group_sources, "Compile", Include=src)

    item_group_refs = ET.SubElement(project, "ItemGroup")
    for dll in dll_references:
        ref = ET.SubElement(
            item_group_refs, "Reference", Include=dll.split("/")[-1].replace(".dll", "")
        )
        hint = ET.SubElement(ref, "HintPath")
        hint.text = dll

    xmlstr = minidom.parseString(ET.tostring(project)).toprettyxml(indent="  ")
    csproj_path.write_text(xmlstr)


def _filter_on_external_dlls(external: Path, paths: Iterable[Path]) -> list[Path]:
    dlls = []
    for path in paths:
        if not str(path).startswith("external"):
            continue
        if (full_path := _find_external_path(external, path)) is not None:
            if (
                full_path.is_file()
                and full_path.suffix == ".dll"
                and full_path not in dlls
            ):
                dlls.append(full_path)
            if full_path.is_dir():
                raise ValueError("Directory found, unexpected")
    return dlls


def compile_csproj(project_name: str, directory: Path, target_framework: str) -> None:
    workspace_absolute = Path(os.environ["BUILD_WORKSPACE_DIRECTORY"])

    directory = workspace_absolute / directory

    actions = _get_actions(directory)

    output_base = _run_bazel_output_base(directory)

    external = output_base / "external"

    dlls = _filter_on_external_dlls(
        external,
        [path for action in actions for path in action.inputs + action.outputs],
    )

    _generate_csproj(
        csproj_path=directory / f"{project_name}.csproj",
        source_files=[],
        dll_references=list(map(str, dlls)),
        target_framework=target_framework,
    )


def _main():
    argument_parser = argparse.ArgumentParser("Compile C# project")
    argument_parser.add_argument("project_name", type=str, help="Name of the project")
    argument_parser.add_argument(
        "directory", type=Path, help="Path to the project directory"
    )
    argument_parser.add_argument(
        "target_framework", type=str, help="Target framework for the project"
    )
    args = argument_parser.parse_args()
    compile_csproj(args.project_name, args.directory, args.target_framework)


if __name__ == "__main__":
    _main()
