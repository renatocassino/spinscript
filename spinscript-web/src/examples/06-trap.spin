// Trap
// A finer grid (grid=32) gives room for fast hi-hat rolls, and the 808 is
// played as a melody so it can follow the bassline.

@bpm = 140;

@kick  = "/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Distorted.wav";
@snare = "/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Trap.wav";
@hat   = "/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 01.wav";
@sub   = "/samples/molla/MOLLA Sounds - 808 Sub/MOLLA Sounds - 808 - Clean (A).wav";
@lead  = "/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Lead (C3) Phonk.wav";

beat @kicks  (sample=@kick, grid=16)  { 1, 4, 11 }
beat @snares (sample=@snare, grid=16) { 9 }
beat @hats   (sample=@hat, grid=32) {
    x.x.x.x.|x.x.x.x.|x.x.x.x.|x.xxxxxx
}

// The 808 sample is an A, so rootNote="A1".
melody @bass (sample=@sub, rootNote="A1") {
    A1 0 3/8,  A1 3/8 1/4,  C2 5/8 3/8,
};

melody @riff (sample=@lead, rootNote="C3") {
    A4 0 1/8, E5 +0 1/8, C5 +0 1/8, _ 1/8, A4 +0 1/8, G4 +0 1/8, A4 +0 1/4,
};

loop @bar {
    play @kicks;
    play @snares;
    play @hats;
    play @bass;
}

loop @drop (bars=2) {
    play @bar (repeat=2);
    play @riff (repeat=2);
}

song {
    play @bar (repeat=2);
    play @drop (repeat=4);
}
