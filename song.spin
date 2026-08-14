@bpm = 129;

pattern @kick(grid=16) {
    3, 6, 9, 12
};

pattern @bass(grid=16) {
    3, 5, 11
};

loop @intro {
    loop @firstPhase {
        loop @instrumental {
            @introMidi = "/intro.mid";

            play @firstPhase (bpm=120, volume=80, repeat=2);
        }
    }
}

song (bpm=120, volume=80) {
    @abuble = 33;
    play @intro;
}