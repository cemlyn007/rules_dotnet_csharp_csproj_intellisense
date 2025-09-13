using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using SymbolAnalysis;

class Action
{
    public int TargetId { get; set; }
    public List<string> Inputs { get; set; } = new List<string>();
    public List<string> Outputs { get; set; } = new List<string>();
    public string PrimaryOutput { get; set; } = "";
}

class ActionsFactory
{
    private readonly JsonDocument _aquery;

    public ActionsFactory(JsonDocument aquery)
    {
        _aquery = aquery;
    }

    public List<Action> GetActions()
    {
        var actions = new List<Action>();
        var aqueryActions = _aquery.RootElement.GetProperty("actions").EnumerateArray();

        foreach (var aqueryAction in aqueryActions)
        {
            if (aqueryAction.GetProperty("mnemonic").GetString() == "CSharpCompile")
            {
                var action = GetAction(aqueryAction.GetProperty("targetId").GetInt32());
                actions.Add(action);
            }
        }
        return actions;
    }

    private string GetArtifactPath(int artifactId)
    {
        var artifacts = _aquery.RootElement.GetProperty("artifacts").EnumerateArray();
        var artifact = artifacts.First(a => a.GetProperty("id").GetInt32() == artifactId);
        var pathFragmentId = artifact.GetProperty("pathFragmentId").GetInt32();
        return GetPath(_aquery.RootElement.GetProperty("pathFragments").EnumerateArray(), pathFragmentId);
    }

    private List<string> RecursivelyGetInputs(int depSetId)
    {
        var inputs = new List<string>();
        var depSetsOfFiles = _aquery.RootElement.GetProperty("depSetOfFiles").EnumerateArray();
        var depSetOfFiles = depSetsOfFiles.First(d => d.GetProperty("id").GetInt32() == depSetId);

        if (depSetOfFiles.TryGetProperty("directArtifactIds", out var directArtifactIds))
        {
            foreach (var directArtifactId in directArtifactIds.EnumerateArray())
            {
                var path = GetArtifactPath(directArtifactId.GetInt32());
                inputs.Add(path);
            }
        }

        if (depSetOfFiles.TryGetProperty("transitiveDepSetIds", out var transitiveDepSetIds))
        {
            foreach (var transitiveDepSetId in transitiveDepSetIds.EnumerateArray())
            {
                inputs.AddRange(RecursivelyGetInputs(transitiveDepSetId.GetInt32()));
            }
        }

        return inputs;
    }

    private Action GetAction(int targetId)
    {
        var inputs = new List<string>();
        var outputs = new List<string>();

        var aqueryActions = _aquery.RootElement.GetProperty("actions").EnumerateArray();
        var action = aqueryActions.First(a =>
            a.GetProperty("targetId").GetInt32() == targetId &&
            a.GetProperty("mnemonic").GetString() == "CSharpCompile");

        if (action.TryGetProperty("inputDepSetIds", out var inputDepSetIds))
        {
            foreach (var inputDepSetId in inputDepSetIds.EnumerateArray())
            {
                var depSetsOfFiles = _aquery.RootElement.GetProperty("depSetOfFiles").EnumerateArray();
                var depSetOfFiles = depSetsOfFiles.First(d => d.GetProperty("id").GetInt32() == inputDepSetId.GetInt32());

                if (depSetOfFiles.TryGetProperty("directArtifactIds", out var directArtifactIds))
                {
                    foreach (var directArtifactId in directArtifactIds.EnumerateArray())
                    {
                        var path = GetArtifactPath(directArtifactId.GetInt32());
                        inputs.Add(path);
                    }
                }

                if (depSetOfFiles.TryGetProperty("transitiveDepSetIds", out var transitiveDepSetIds))
                {
                    foreach (var transitiveDepSetId in transitiveDepSetIds.EnumerateArray())
                    {
                        var paths = RecursivelyGetInputs(transitiveDepSetId.GetInt32());
                        inputs.AddRange(paths);
                    }
                }
            }
        }

        if (action.TryGetProperty("outputIds", out var outputIds))
        {
            foreach (var outputId in outputIds.EnumerateArray())
            {
                var path = GetArtifactPath(outputId.GetInt32());
                outputs.Add(path);
            }
        }

        var primaryOutputId = action.GetProperty("primaryOutputId").GetInt32();
        var primaryOutputPath = GetArtifactPath(primaryOutputId);

        // Filter out DOTNET_CLI_HOME paths
        string dotnetCliHome = "";
        if (action.TryGetProperty("environmentVariables", out var envVars))
        {
            foreach (var envVar in envVars.EnumerateArray())
            {
                if (envVar.GetProperty("key").GetString() == "DOTNET_CLI_HOME")
                {
                    dotnetCliHome = envVar.GetProperty("value").GetString();
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(dotnetCliHome))
        {
            inputs = inputs.Where(input => !input.StartsWith(dotnetCliHome)).ToList();
        }

        return new Action
        {
            TargetId = targetId,
            Inputs = inputs,
            Outputs = outputs,
            PrimaryOutput = primaryOutputPath
        };
    }

    private static string GetPath(JsonElement.ArrayEnumerator pathFragments, int pathFragmentId)
    {
        var fragment = pathFragments.First(f => f.GetProperty("id").GetInt32() == pathFragmentId);
        var relatedFragments = new List<string> { fragment.GetProperty("label").GetString()! };

        while (fragment.TryGetProperty("parentId", out var parentIdElement))
        {
            var parentId = parentIdElement.GetInt32();
            fragment = pathFragments.First(f => f.GetProperty("id").GetInt32() == parentId);
            relatedFragments.Add(fragment.GetProperty("label").GetString()!);
        }

        relatedFragments.Reverse();
        return Path.Combine(relatedFragments.ToArray());
    }
}

class CompileCsproj
{
    const string BazelBinary = "bazel";

    static string RunProcess(string[] command, string cwd)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command[0],
            Arguments = string.Join(' ', command.Skip(1)),
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        string output = proc.StandardOutput.ReadToEnd();
        string error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception($"Command '{string.Join(' ', command)}' failed.\nStdout: {output}\nStderr: {error}");
        return output;
    }

    static JsonDocument RunBazelAquery(string directory, params string[] targets)
    {
        string queryExpression;

        // Default to all targets if none specified
        if (targets.Length == 0)
        {
            queryExpression = "//...";
        }
        else if (targets.Length == 1)
        {
            queryExpression = targets[0];
        }
        else
        {
            // For multiple targets, use a union expression: "target1 + target2 + target3"
            queryExpression = string.Join(" + ", targets);
        }

        var command = new[] { BazelBinary, "aquery", queryExpression, "--output=jsonproto" };

        string output = RunProcess(command, directory);
        return JsonDocument.Parse(output);
    }

    static string RunBazelInfo(string directory, string info)
    {
        return RunProcess(new[] { BazelBinary, "info", info }, directory).Trim();
    }



    static List<Action> GetActions(string directory, params string[] targets)
    {
        var aquery = RunBazelAquery(directory, targets);
        var actionsFactory = new ActionsFactory(aquery);
        return actionsFactory.GetActions();
    }

    static void CompileCsProj(string projectName, string directory, string targetFramework, params string[] targets)
    {
        string workspaceAbsolute = Environment.GetEnvironmentVariable("BUILD_WORKSPACE_DIRECTORY") ?? "";
        if (!string.IsNullOrEmpty(workspaceAbsolute))
            directory = Path.Combine(workspaceAbsolute, directory);

        var actions = GetActions(directory, targets);
        string outputBase = RunBazelInfo(directory, "output_base");
        string bazelOutputPath = RunBazelInfo(directory, "output_path");
        string bazelWorkspace = RunBazelInfo(directory, "workspace");

        string external = Path.Combine(outputBase, "external");
        var prefixes = new[]
        {
            Path.GetDirectoryName(external)!,
            bazelWorkspace,
            Path.GetDirectoryName(bazelOutputPath)!
        };

        var paths = new List<string>();
        foreach (var action in actions)
        {
            var allPaths = action.Inputs.Concat(action.Outputs);
            foreach (var path in allPaths)
            {
                var extension = Path.GetExtension(path);
                if (extension == ".cs" || extension == ".dll")
                {
                    foreach (var prefix in prefixes)
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(prefix, path));
                        if (File.Exists(fullPath))
                        {
                            paths.Add(fullPath);
                            break;
                        }
                    }
                }
            }
        }

        var groupings = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        Parallel.ForEach(paths, path =>
        {
            try
            {
                var symbol = SymbolLister.AnalyzeFile(path);
                if (symbol != null)
                {
                    groupings.AddOrUpdate(symbol,
                        new ConcurrentBag<string> { path },
                        (key, existing) => { existing.Add(path); return existing; });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to get symbols for {path}: {ex.Message}");
            }
        });

        var selectedDlls = new HashSet<string>();
        foreach (var (symbol, pathsBag) in groupings)
        {
            var pathsList = pathsBag.ToList();
            if (pathsList.Any(path => Path.GetExtension(path) == ".cs"))
                continue;

            var dll = pathsList.FirstOrDefault(path => Path.GetExtension(path) == ".dll");
            if (!string.IsNullOrEmpty(dll))
                selectedDlls.Add(dll);
        }

        GenerateCsproj(
            Path.Combine(directory, $"{projectName}.csproj"),
            new List<string>(), // Source files - empty as in Python
            selectedDlls.ToList(),
            targetFramework
        );
    }

    static void GenerateCsproj(string csprojPath, List<string> sourceFiles, List<string> dllReferences, string targetFramework)
    {
        var project = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"));
        var propGroup = new XElement("PropertyGroup");
        propGroup.Add(new XElement("TargetFramework", targetFramework));
        project.Add(propGroup);

        if (sourceFiles.Count > 0)
        {
            var itemGroupSources = new XElement("ItemGroup");
            foreach (var src in sourceFiles)
                itemGroupSources.Add(new XElement("Compile", new XAttribute("Include", src)));
            project.Add(itemGroupSources);
        }

        if (dllReferences.Count > 0)
        {
            var itemGroupRefs = new XElement("ItemGroup");
            foreach (var dll in dllReferences)
            {
                var refElem = new XElement("Reference", new XAttribute("Include", Path.GetFileNameWithoutExtension(dll)));
                refElem.Add(new XElement("HintPath", dll));
                itemGroupRefs.Add(refElem);
            }
            project.Add(itemGroupRefs);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), project);
        doc.Save(csprojPath);
    }

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: CompileCsproj <project_name> <directory> <target_framework> [targets...]");
            Console.WriteLine("If no targets are specified, defaults to '//...'");
            return;
        }
        string projectName = args[0];
        string directory = args[1];
        string targetFramework = args[2];
        string[] targets = args.Skip(3).ToArray(); // Additional targets if provided

        CompileCsProj(projectName, directory, targetFramework, targets);
    }
}