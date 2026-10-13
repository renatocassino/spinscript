namespace SpinScript.Music;

/// <summary>
/// Named chord qualities, as semitone intervals above the root. Used by the
/// <c>ROOT:quality</c> chord syntax in melodies, e.g. <c>C4:maj7</c> →
/// C4 E4 G4 B4. Names are case-sensitive (<c>m</c> is minor).
/// </summary>
public static class Chords
{
    public static readonly IReadOnlyDictionary<string, int[]> Qualities = new Dictionary<string, int[]>
    {
        // triads
        ["maj"] = [0, 4, 7],
        ["m"] = [0, 3, 7],
        ["min"] = [0, 3, 7],
        ["dim"] = [0, 3, 6],
        ["aug"] = [0, 4, 8],
        ["sus2"] = [0, 2, 7],
        ["sus4"] = [0, 5, 7],
        ["5"] = [0, 7],

        // sixths and sevenths
        ["6"] = [0, 4, 7, 9],
        ["m6"] = [0, 3, 7, 9],
        ["7"] = [0, 4, 7, 10],
        ["maj7"] = [0, 4, 7, 11],
        ["m7"] = [0, 3, 7, 10],
        ["mmaj7"] = [0, 3, 7, 11],
        ["dim7"] = [0, 3, 6, 9],
        ["m7b5"] = [0, 3, 6, 10],
        ["aug7"] = [0, 4, 8, 10],
        ["7sus4"] = [0, 5, 7, 10],

        // extensions
        ["add9"] = [0, 4, 7, 14],
        ["9"] = [0, 4, 7, 10, 14],
        ["maj9"] = [0, 4, 7, 11, 14],
        ["m9"] = [0, 3, 7, 10, 14],
    };

    /// <summary>
    /// The chord's notes from the root up, e.g. (<c>"A3"</c>, <c>"m"</c>) →
    /// <c>["A3", "C4", "E4"]</c>. Returns <c>null</c> for an unknown quality.
    /// </summary>
    public static List<string>? Expand(string root, string quality)
    {
        if (!Qualities.TryGetValue(quality, out var intervals))
        {
            return null;
        }

        // The root keeps the spelling it was written with (Bb3, not A#3).
        var rootMidi = Pitch.ToMidi(root);
        return intervals.Select(interval => interval == 0 ? root : Pitch.FromMidi(rootMidi + interval)).ToList();
    }
}
