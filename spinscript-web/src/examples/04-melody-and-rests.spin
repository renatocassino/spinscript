// Melody and rests
// "+0" starts a note right where the previous one ends, so you never have
// to add up start times by hand. "_ 1/4" is a rest: a quarter of silence.

@bpm = 96;

@piano = "/samples/tonal/piano-c3.wav";

melody @tune (sample=@piano, rootNote="C3") {
    // NOTE  START  LENGTH  (1/4 = one beat)
    E4  0   1/4,  E4 +0 1/4,  F4 +0 1/4,  G4 +0 1/4,
    G4  +0  1/4,  F4 +0 1/4,  E4 +0 1/4,  D4 +0 1/4,
    C4  +0  1/4,  C4 +0 1/4,  D4 +0 1/4,  E4 +0 1/4,
    E4  +0  3/8,  D4 +0 1/8,  D4 +0 1/4,
    _ 1/4,                                   // breathe
    E4  +0  1/4,  E4 +0 1/4,  F4 +0 1/4,  G4 +0 1/4,
    G4  +0  1/4,  F4 +0 1/4,  E4 +0 1/4,  D4 +0 1/4,
    C4  +0  1/4,  C4 +0 1/4,  D4 +0 1/4,  E4 +0 1/4,
    D4  +0  3/8,  C4 +0 1/8,  C4 +0 1/2,
};

song {
    play @tune;
}
