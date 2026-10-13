// Chord progression
// I – V – vi – IV in C major, the progression behind countless pop songs.
// A chord is several notes that start together: write them in [ ] or
// by name, ROOT:quality (C4:maj, G3:maj, A3:m, F3:maj...).

@bpm = 90;

@pad  = "/samples/tonal/pad-c3.wav";
@kick = "/samples/kick/subby.wav";
@clap = "/samples/clap/wet.wav";

// The pad sample is a recording of C3, so rootNote="C3".
// Start and length are fractions of a bar: 0 = top of bar 1, 1 = bar 2...
melody @chords (sample=@pad, rootNote="C3") {
    [C4 E4 G4]  0  1,      // C major, written note by note
    G3:maj      1  1,      // G major
    A3:m        2  1,      // A minor
    F3:maj      3  1,      // F major
};

beat @kicks (sample=@kick) { 1, 9 }
beat @claps (sample=@clap) { 5, 13 }

// Beats and melodies inside a loop all start together, at the top of it.
// The chords last four bars; the one-bar beats repeat to keep up.
loop @verse (bars=4) {
    play @chords;
    play @kicks (repeat=4);
    play @claps (repeat=4);
}

song {
    play @verse (repeat=2);
}
