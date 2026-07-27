using System;
using System.IO;
using UnityEngine;

// A single predetermined trial. Field names must match the JSON keys exactly
// (Unity's JsonUtility is case-sensitive and maps by field name).
[Serializable]
public class TrialSpec
{
    public string trialType;   // e.g. "HighLow" (matches General.trialTypes entries)
    public int separation;     // index distance between the two walls (1/2/4)
    public int direction;      // +1 (CCW) or -1 (CW) offset of the low wall from the anchor
    public int anchorWallID;   // custom ID of the anchor wall (== highWallID)
    public int highWallID;     // custom ID assigned as wall1 (high / anchor)
    public int lowWallID;      // custom ID assigned as wall2 (low / dependent)

    // Per-trial timing (seconds), predetermined so inference is fully reproducible.
    // Consumed by OctagonArenaSettings.ITI() for the gap PRECEDING this trial.
    // Older sequence files omit these keys -> JsonUtility deserialises them to 0 ->
    // the arena falls back to its normal Random.Range draw (see ITI()), so standard
    // behaviour is preserved for both random generation and legacy files.
    public float iti;             // inter-trial interval (General.ITIMin..ITIMax)
    public float trialStartDelay; // post-ITI start delay (0.5..1.5)
}

// Wrapper object so JsonUtility can deserialise the top-level file
// (JsonUtility cannot parse a bare top-level JSON array).
[Serializable]
public class TrialSequenceFile
{
    public int meta_seed;
    public int meta_numWalls;
    public int meta_generatedTrials;
    public TrialSpec[] trials;
}

/// <summary>
/// Loads a predetermined sequence of trials from a JSON file and hands them out
/// one-per-trial, so an inference run replays an identical string of trials
/// (matching the statistics defined in Globals.General) regardless of the
/// in-engine RNG. When no file is supplied the arena falls back to random
/// generation. Global/static because inference uses a single arena with a single
/// PlayerAgent driving trial setup.
/// </summary>
public static class ScriptedTrialSequence
{
    private static TrialSpec[] _trials;
    private static int _index;

    /// <summary>True when a valid sequence file has been loaded.</summary>
    public static bool Enabled { get; private set; }

    /// <summary>Number of trials in the loaded sequence (0 if none).</summary>
    public static int Count => _trials?.Length ?? 0;

    /// <summary>Index of the next trial to hand out.</summary>
    public static int Index => _index;

    /// <summary>True once every trial in the sequence has been consumed.</summary>
    public static bool Exhausted => Enabled && _index >= Count;

    /// <summary>Path the sequence was loaded from (null if not loaded).</summary>
    public static string SourcePath { get; private set; }

    /// <summary>
    /// Load a sequence from <paramref name="path"/>. Any failure (missing path,
    /// missing file, parse error, empty sequence) leaves Enabled == false so the
    /// caller silently falls back to the existing random generation.
    /// </summary>
    public static void Load(string path)
    {
        Enabled = false;
        _trials = null;
        _index = 0;
        SourcePath = null;

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[ScriptedTrialSequence] No --trial_seq supplied; using random trial generation.");
            return;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"[ScriptedTrialSequence] Sequence file not found: {path}. Falling back to random generation.");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            TrialSequenceFile file = JsonUtility.FromJson<TrialSequenceFile>(json);
            if (file == null || file.trials == null || file.trials.Length == 0)
            {
                Debug.LogError($"[ScriptedTrialSequence] No trials parsed from {path}. Falling back to random generation.");
                return;
            }

            _trials = file.trials;
            SourcePath = path;
            Enabled = true;
            Debug.Log($"[ScriptedTrialSequence] Loaded {_trials.Length} scripted trials from {path} (seed {file.meta_seed}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScriptedTrialSequence] Failed to parse {path}: {e}. Falling back to random generation.");
        }
    }

    /// <summary>
    /// Return the current trial and advance the cursor. Returns null when disabled
    /// or exhausted (callers should stop the sim before this happens).
    /// </summary>
    public static TrialSpec Next()
    {
        if (!Enabled || _index >= _trials.Length) return null;
        return _trials[_index++];
    }

    /// <summary>
    /// Return the trial that the next Next() call will hand out, WITHOUT advancing
    /// the cursor. Used to read the upcoming trial's predetermined timing during
    /// the ITI that precedes it (ITI is drawn before SetUpArena consumes the trial).
    /// Returns null when disabled or exhausted.
    /// </summary>
    public static TrialSpec Peek()
    {
        if (!Enabled || _index >= _trials.Length) return null;
        return _trials[_index];
    }

    /// <summary>Restart the sequence from the first trial.</summary>
    public static void Reset() => _index = 0;
}
