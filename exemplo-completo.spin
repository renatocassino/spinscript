@bpm = 129;

// ===================================================================
// SAMPLES (arquivos de áudio que a música vai usar)
// O interpretador deduz o que baixar a partir daqui.
// ===================================================================

@kick   = "https://cdn.spin.dev/samples/kick_808.wav";
@hihat  = "https://cdn.spin.dev/samples/hihat_closed.wav";
@piano  = "https://cdn.spin.dev/instruments/grand_piano_c4.wav";
@vinil  = "https://cdn.spin.dev/loops/vinyl_crackle_bnaz.mp3";

// ===================================================================
// CATEGORIA A — percussão (sample disparado numa grade de steps)
// Sem altura, sem duração: o step liga, o sample toca.
// ===================================================================

pattern @beat (sample=@kick, grid=16) {
    1, 5, 9, 13
};

pattern @chimbal (sample=@hihat, grid=16) {
    3, 7, 11, 15
};

// ===================================================================
// CATEGORIA B — melodia (notas com altura e duração, tempo livre)
// A nota tem: qual é (C4), quanto dura (1/4) e onde começa (offset).
// O som vem de um wav de piano, afinado por nota.
// ===================================================================

pattern @melodia (instrument=@piano, free=false) {
    // nota  duracao  inicio
    // E4       1/4      0
    // G4       1/4      1/4
    // C5       1/2      1/2
    // B4       1/4      1
    // A4       1/4      5/4
    // G4       1/2      3/2
};

// ===================================================================
// CATEGORIA C — faixa contínua (um mp3 inteiro tocando como camada)
// Nada de steps nem notas: dispara o arquivo a partir de um ponto.
// ===================================================================

// (usado direto no loop com play @vinil)

// ===================================================================
// LOOPS — empilham patterns; tudo aqui toca simultaneamente
// ===================================================================

loop @groove {
    play @beat;
    play @chimbal;
}

loop @intro (bars=4) {
    play @vinil;              // vinil crackle rolando por baixo
    play @groove;             // a batida entra por cima
}

loop @verso (bars=8) {
    play @groove;
    play @melodia;            // a melodia do piano entra no verso
}

// ===================================================================
// SONG — o roteiro da faixa
// ===================================================================

song {
    play @intro;
    play @verso (repeat=2);
    play @groove (repeat=4);
}
