// Boom bap
// The same idea as "My first beat", written as drum-machine rows:
// each character is one step, x plays, . is silence, | is just a divider.

@bpm = 88;

@kick  = "/samples/kick/lo-fi.wav";
@snare = "/samples/snare/boom-bap.wav";
@hat   = "/samples/hihat/closed-02.wav";
@open  = "/samples/hihat/open-01.wav";
@rim   = "/samples/perc/rimshot.wav";

beat @kickA  (sample=@kick)  { x...|....|..x.|.x.. }
beat @kickB  (sample=@kick)  { x...|...x|..x.|.... }
beat @snare1 (sample=@snare) { ....|x...|....|x... }
beat @hats   (sample=@hat)   { x.x.|x.x.|x.x.|x... }
beat @openHat (sample=@open) { ....|....|....|..x. }
beat @rims   (sample=@rim)   { ....|..x.|....|.... }

loop @barA {
    play @kickA;
    play @snare1;
    play @hats;
}

loop @barB {
    play @kickB;
    play @snare1;
    play @hats;
    play @openHat;
    play @rims;
}

// Loops inside a loop play one after the other: A, B, A, B.
loop @phrase {
    play @barA;
    play @barB;
}

song {
    play @phrase (repeat=4);
}
