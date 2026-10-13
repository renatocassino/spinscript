// Full arrangement
// The song block is the script of the track: intro, verse, chorus, outro.
// Each section is a loop; sections play one after the other.

@bpm = 110;

@kick  = "/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Clean.wav";
@snare = "/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Funk.wav";
@hat   = "/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 03.wav";
@conga = "/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Conga 01.wav";
@piano = "/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Piano (C3).wav";
@pad   = "/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Pad (C3).wav";

beat @kicks  (sample=@kick)  { 1, 7, 9, 11 }
beat @snares (sample=@snare) { 5, 13 }
beat @hats   (sample=@hat)   { x.x.|x.x.|x.x.|x.xx }
beat @congas (sample=@conga) { ..x.|..x.|x...|..x. }

melody @padChords (sample=@pad, rootNote="C3") {
    A3:m7 0 2,  F3:maj7 2 2,
};

melody @hook (sample=@piano, rootNote="C3") {
    A4 0 1/8, C5 +0 1/8, E5 +0 1/4, _ 1/8, D5 +0 1/8, C5 +0 1/4,
    _ 1/4,
    G4 +0 1/8, A4 +0 1/8, C5 +0 1/4, A4 +0 1/2,
};

loop @intro (bars=4) {
    play @padChords (repeat=2);
    play @congas (repeat=4);
}

// Beats and melodies inside a loop all start together, at the top of it.
loop @verse (bars=4) {
    play @padChords (repeat=2);
    play @kicks (repeat=4);
    play @snares (repeat=4);
    play @hats (repeat=4);
}

loop @chorus (bars=4) {
    play @padChords (repeat=2);
    play @kicks (repeat=4);
    play @snares (repeat=4);
    play @hats (repeat=4);
    play @congas (repeat=4);
    play @hook (repeat=2);
}

loop @outro (bars=2) {
    play @padChords;
}

song {
    play @intro;
    play @verse;
    play @chorus;
    play @verse;
    play @chorus (repeat=2);
    play @outro;
}
