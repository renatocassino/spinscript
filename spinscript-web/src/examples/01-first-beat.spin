// My first beat
// A kick on every beat, a snare on 2 and 4, and hi-hats in between.
// Press Play, then try changing the numbers inside the { }.

@bpm = 100;

@kick  = "/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Punchy.wav";
@snare = "/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Tight.wav";
@hat   = "/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 01.wav";

// A bar has 16 steps (grid=16): 1, 5, 9 and 13 are the four beats.
beat @kickPart  (sample=@kick)  { 1, 5, 9, 13 }
beat @snarePart (sample=@snare) { 5, 13 }
beat @hatPart   (sample=@hat)   { 3, 7, 11, 15 }

// Everything inside a loop plays at the same time.
loop @groove (bars=1) {
    play @kickPart;
    play @snarePart;
    play @hatPart;
}

song {
    play @groove (repeat=4);
}
