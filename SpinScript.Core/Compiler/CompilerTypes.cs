namespace SpinScript.Compiler;

public abstract record Event;

public record SoundEvent(string Sample, double Time, int Velocity) : Event;

public record MelodyEvent(string Sample, double Time, double Duration, string Note, double rate) : Event;

public record CompileResult(List<Event> Events);

// ---------------------------------------------------------------------------
// TIMING VOCABULARY
//
// These terms describe how musical time is organized in SpinScript. They form
// a hierarchy, from the largest unit to the smallest:
//
//   BAR (also "measure")
//     A single block of musical time, made up of a fixed number of beats.
//     One row of steps in the sequencer represents one bar.
//
//   BEAT
//     The pulse you'd tap your foot to. A bar contains BeatsPerBar beats
//     (4 by default, i.e. common 4/4 time). At a given BPM, one beat lasts
//     60 / BPM seconds.
//
//   STEP
//     The smallest clickable slot in a pattern's grid, where a sound can
//     fire. Steps are grouped into beats: with a grid of 16 and 4 beats per
//     bar, each beat spans 4 steps (steps 1-4 = beat 1, 5-8 = beat 2, etc.).
//
//   GRID
//     How many steps make up one bar for a given pattern (its resolution).
//     A grid of 16 splits one bar into 16 steps (sixteenth notes). Higher
//     grid = finer resolution, not longer duration.
//
// KEY RELATIONSHIP (the common case):
//   1 bar = BeatsPerBar beats = grid steps
//   e.g. 1 bar = 4 beats = 16 steps  ->  each step = 1/4 of a beat
//
// GLOBAL vs PER-PATTERN:
//   - BPM and BeatsPerBar are GLOBAL (properties of the whole song).
//   - GRID is PER-PATTERN (each pattern chooses its own resolution).
//
// DERIVED TIMINGS:
//   beatMs = 60000 / BPM
//   barMs  = beatMs * BeatsPerBar
//   stepMs = barMs / grid
// ---------------------------------------------------------------------------

public record SongConfiguration(int BPM, int beatsPerBar = 4)
{
    public double BeatDurationMs => 60.0 / BPM * 1000.0;
    public double BarDurationMs => BeatDurationMs * beatsPerBar;
};
