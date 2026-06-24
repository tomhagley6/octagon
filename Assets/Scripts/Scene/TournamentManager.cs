using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Sentis;
using UnityEngine;

/// <summary>
/// Registry of candidate models for headless tournament inference runs.
///
/// A Python driver launches the build once per matchup, passing
///     --player_model &lt;name&gt;  --opp_model &lt;name&gt;
/// where &lt;name&gt; matches an imported ModelAsset's name (the .onnx filename without its
/// extension, e.g. "OctagonAgentSocial_260126_04"). Each <see cref="OctagonAgent"/> reads
/// the arg for its tag in Initialize() and calls SetModel(), so no scene editing is needed
/// between matchups.
///
/// This component also writes tournament_manifest.json into the --sim_out directory,
/// recording which model was actually applied to each role, so downstream analysis can
/// recover the matchup (and verify it was applied) without relying on the output folder name.
///
/// Attach to a single GameObject in the tournament scene and drag every entrant ModelAsset
/// into <see cref="entrantModels"/>.
/// </summary>
public class TournamentManager : MonoBehaviour
{
    [Tooltip("Every model that may take part in a tournament. The CLI --player_model / " +
             "--opp_model names are matched (case-sensitive) against these assets' names.")]
    public List<ModelAsset> entrantModels = new List<ModelAsset>();

    // The tournament scene contains exactly one of these; agents look it up in Initialize().
    public static TournamentManager Instance { get; private set; }

    // tag -> model name actually applied via SetModel(). Populated by agents in Initialize().
    private static readonly Dictionary<string, string> s_Assignments = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TournamentManager] Multiple instances found; destroying the extra one.");
            Destroy(this);
            return;
        }
        Instance = this;
        s_Assignments.Clear();
    }

    /// <summary>Resolve an entrant model by its asset name, or null (with an error) if absent.</summary>
    public ModelAsset GetModelByName(string modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return null;

        foreach (var m in entrantModels)
        {
            if (m != null && m.name == modelName) return m;
        }

        var available = new List<string>();
        foreach (var m in entrantModels) available.Add(m != null ? m.name : "<null>");
        Debug.LogError($"[TournamentManager] No entrant model named '{modelName}'. " +
                       $"Add it to entrantModels in the inspector. Available: {string.Join(", ", available)}");
        return null;
    }

    /// <summary>Called by an agent once it has applied a model, for the run manifest.</summary>
    public void RegisterAssignment(string agentTag, string modelName)
    {
        s_Assignments[agentTag] = modelName;
    }

    void Start()
    {
        StartCoroutine(WriteManifestWhenReady());
    }

    // Wait a couple of frames so every agent's Initialize() has run and registered its
    // assignment, then dump the manifest alongside DONE.txt in the simulation output dir.
    IEnumerator WriteManifestWhenReady()
    {
        yield return null;
        yield return null;

        var args = Environment.GetCommandLineArgs();
        string simOut = GetArg(args, "--sim_out");
        if (string.IsNullOrEmpty(simOut))
        {
            // Editor / interactive run: nothing to write; assignments are still logged to console.
            yield break;
        }

        string playerArg = GetArg(args, "--player_model");
        string oppArg = GetArg(args, "--opp_model");

        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"player_model_arg\": {JsonStr(playerArg)},\n");
        sb.Append($"  \"opp_model_arg\": {JsonStr(oppArg)},\n");
        sb.Append("  \"applied\": {\n");
        int i = 0;
        foreach (var kv in s_Assignments)
        {
            sb.Append($"    {JsonStr(kv.Key)}: {JsonStr(kv.Value)}");
            sb.Append(++i < s_Assignments.Count ? ",\n" : "\n");
        }
        sb.Append("  }\n");
        sb.Append("}\n");

        try
        {
            Directory.CreateDirectory(simOut);
            File.WriteAllText(Path.Combine(simOut, "tournament_manifest.json"), sb.ToString());
            Debug.Log($"[TournamentManager] Wrote tournament_manifest.json to {simOut}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TournamentManager] Failed to write manifest: {e}");
        }
    }

    static string JsonStr(string s) =>
        s == null ? "null" : "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    static string GetArg(string[] args, string name)
    {
        int idx = Array.IndexOf(args, name);
        if (idx >= 0 && idx < args.Length - 1) return args[idx + 1];
        return null;
    }
}
