namespace SpinScript.Music;

using System.Globalization;

/// <summary>
/// Conversions between note names (<c>C4</c>, <c>F#3</c>, <c>Bb5</c>) and
/// MIDI note numbers, where C4 = 60.
/// </summary>
public static class Pitch
{
    private static readonly Dictionary<char, int> LetterOffsets = new()
    {
        ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5,
        ['G'] = 7, ['A'] = 9, ['B'] = 11,
    };

    public const int DefaultOctave = 4;

    private static readonly string[] SharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    /// <summary>
    /// <c>C4</c>, <c>G#5</c>, <c>Db3</c>... The octave is optional and
    /// defaults to 4, so <c>Db</c> is the same as <c>Db4</c>.
    /// </summary>
    public static int ToMidi(string note)
    {
        var offset = LetterOffsets[note[0]];
        var octaveIndex = 1;

        if (note.Length > 1 && note[1] == '#') { offset += 1; octaveIndex = 2; }
        else if (note.Length > 1 && note[1] == 'b') { offset -= 1; octaveIndex = 2; }

        var octave = octaveIndex < note.Length
            ? int.Parse(note[octaveIndex..], CultureInfo.InvariantCulture)
            : DefaultOctave;

        return (octave + 1) * 12 + offset;
    }

    /// <summary>Spells black keys as sharps: 61 → <c>C#4</c>.</summary>
    public static string FromMidi(int midi) =>
        $"{SharpNames[((midi % 12) + 12) % 12]}{(int)Math.Floor(midi / 12.0) - 1}";
}
