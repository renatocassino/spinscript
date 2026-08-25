// ===================================================================
// minha-autoria.spin
// Transcrição da melodia original (solfejo) para SpinScript.
// Convenção usada na transcrição original: nota em MAIÚSCULA = oitava 5,
// minúscula = oitava 4; "#" indica sustenido; "." é pausa; "-" é ligadura
// (estende a nota anterior por mais um tempo).
// ===================================================================

@bpm = 100;

@piano = "/PIANO-C3.wav";

// ===================================================================
// MELODIA — cada nota tem: qual é, onde começa e quanto dura, em
// frações de compasso (beatsPerBar=4, então 1/4 = uma batida).
// ===================================================================

melody @minhaAutoria (sample=@piano) {

    // ==== Tema ====
    // SOL FA SOL MI
    G5 0/4 1/4, F5 +0 1/4, G5 +0 1/4, E5 +0 1/4,
    // SOL RE SOL
    G5 +0 1/4, D5 +0 1/4, G5 +0 1/4,

    // ==== Parte A ====
    // SOL SOL MI FA SOL LA SOL FA MI RE
    G5 +0 1/4, G5 +0 1/4, E5 +0 1/4, F5 +0 1/4, G5 +0 1/4, A5 +0 1/4, G5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4,
    // MI MI DO RE MI FA MI RE DO si
    E5 +0 1/4, E5 +0 1/4, C5 +0 1/4, D5 +0 1/4, E5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4, C5 +0 1/4, B4 +0 1/4,
    // la la DO LA SOL DO
    A4 +0 1/4, A4 +0 1/4, C5 +0 1/4, A5 +0 1/4, G5 +0 1/4, C5 +0 1/4,
    // RE MI FA MI RE DO RE RE DO si
    D5 +0 1/4, E5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4, C5 +0 1/4, D5 +0 1/4, D5 +0 1/4, C5 +0 1/4, B4 +0 1/4,

    // ==== Parte B ====
    // DO SOL# SOL FA RE# RE RE#
    C5 +0 1/4, G#5 +0 1/4, G5 +0 1/4, F5 +0 1/4, D#5 +0 1/4, D5 +0 1/4, D#5 +0 1/4,
    // RE LA# SOL# SOL FA RE# FA MI
    D5 +0 1/4, A#5 +0 1/4, G#5 +0 1/4, G5 +0 1/4, F5 +0 1/4, D#5 +0 1/4, F5 +0 1/4, E5 +0 1/4,
    // sol fa sol mi sol
    G4 +0 1/4, F4 +0 1/4, G4 +0 1/4, E4 +0 1/4, G4 +0 1/4,

    // ==== Parte C ====
    // DO DO DO DO DO
    C5 +0 1/4, C5 +0 1/4, C5 +0 1/4, C5 +0 1/4, C5 +0 1/4,
    // la# sol# la# DO la#
    A#4 +0 1/4, G#4 +0 1/4, A#4 +0 1/4, C5 +0 1/4, A#4 +0 1/4,
    // do# la# la# la# sol# sol# la# sol# sol sol# la# sol#
    C#4 +0 1/4, A#4 +0 1/4, A#4 +0 1/4, A#4 +0 1/4, G#4 +0 1/4, G#4 +0 1/4, A#4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, G#4 +0 1/4, A#4 +0 1/4, G#4 +0 1/4,
    // do sol# sol# sol# sol sol fa sol fa fa fa fa fa fa re# fa sol
    C4 +0 1/4, G#4 +0 1/4, G#4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, G4 +0 1/4, F4 +0 1/4, G4 +0 1/4, F4 +0 1/4, F4 +0 1/4, F4 +0 1/4, F4 +0 1/4, F4 +0 1/4, F4 +0 1/4, D#4 +0 1/4, F4 +0 1/4, G4 +0 1/4,

    // ==== Parte D ====
    // sol# sol# la# DO
    G#4 +0 1/4, G#4 +0 1/4, A#4 +0 1/4, C5 +0 1/4,
    // DO DO DO . DO DO DO   (pausa de uma batida no meio)
    C5 +0 1/4, C5 +0 1/4, C5 +0 1/4, C5 +1/4 1/4, C5 +0 1/4, C5 +0 1/4,
    // la# sol# la# DO la#
    A#4 +0 1/4, G#4 +0 1/4, A#4 +0 1/4, C5 +0 1/4, A#4 +0 1/4,

    // ==== Parte E ====
    // la# la# DO RE RE# RE RE# la# sol# sol# sol sol#
    A#4 +0 1/4, A#4 +0 1/4, C5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, A#4 +0 1/4, G#4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, G#4 +0 1/4,
    // sol# la# sol# sol sol
    G#4 +0 1/4, A#4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, G4 +0 1/4,
    // sol sol SOL FA FA FA RE# RE RE#
    G4 +0 1/4, G4 +0 1/4, G5 +0 1/4, F5 +0 1/4, F5 +0 1/4, F5 +0 1/4, D#5 +0 1/4, D5 +0 1/4, D#5 +0 1/4,

    // ==== Parte F ====
    // DO DO RE RE# RE#
    C5 +0 1/4, C5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, D#5 +0 1/4,
    // RE RE RE# – RE RE#   (a nota ligada dura duas batidas)
    D5 +0 1/4, D5 +0 1/4, D#5 +0 2/4, D5 +0 1/4, D#5 +0 1/4,
    // DO FA RE# RE DO DO la#
    C5 +0 1/4, F5 +0 1/4, D#5 +0 1/4, D5 +0 1/4, C5 +0 1/4, C5 +0 1/4, A#4 +0 1/4,
    // sol sol la# la# – sol sol la# la#   (a nota ligada dura duas batidas)
    G4 +0 1/4, G4 +0 1/4, A#4 +0 1/4, A#4 +0 2/4, G4 +0 1/4, G4 +0 1/4, A#4 +0 1/4, A#4 +0 1/4,
    // sol sol la# DO RE RE# RE RE# DO
    G4 +0 1/4, G4 +0 1/4, A#4 +0 1/4, C5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, C5 +0 1/4,
    // RE# RE RE# DO DO SOL FA FA
    D#5 +0 1/4, D5 +0 1/4, D#5 +0 1/4, C5 +0 1/4, C5 +0 1/4, G5 +0 1/4, F5 +0 1/4, F5 +0 1/4,
    // FA FA RE# FA SOL
    F5 +0 1/4, F5 +0 1/4, D#5 +0 1/4, F5 +0 1/4, G5 +0 1/4,

    // ==== Reprise do tema principal ====
    // SOL SOL MI FA SOL LA SOL FA MI RE
    G5 +0 1/4, G5 +0 1/4, E5 +0 1/4, F5 +0 1/4, G5 +0 1/4, A5 +0 1/4, G5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4,
    // MI MI DO RE MI FA MI RE DO si
    E5 +0 1/4, E5 +0 1/4, C5 +0 1/4, D5 +0 1/4, E5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4, C5 +0 1/4, B4 +0 1/4,
    // la la DO LA SOL DO
    A4 +0 1/4, A4 +0 1/4, C5 +0 1/4, A5 +0 1/4, G5 +0 1/4, C5 +0 1/4,
    // RE MI FA MI RE DO RE RE DO si
    D5 +0 1/4, E5 +0 1/4, F5 +0 1/4, E5 +0 1/4, D5 +0 1/4, C5 +0 1/4, D5 +0 1/4, D5 +0 1/4, C5 +0 1/4, B4 +0 1/4,

    // ==== Coda ====
    // do sol# sol fa re# re re#
    C4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, F4 +0 1/4, D#4 +0 1/4, D4 +0 1/4, D#4 +0 1/4,
    // re la# sol# sol fa re# fa
    D4 +0 1/4, A#4 +0 1/4, G#4 +0 1/4, G4 +0 1/4, F4 +0 1/4, D#4 +0 1/4, F4 +0 1/4,
    // FA SOL FA MI
    F5 +0 1/4, G5 +0 1/4, F5 +0 1/4, E5 +0 1/4,
};

// ===================================================================
// LOOP — a composição inteira como um único trecho (62 compassos:
// 247 batidas arredondadas para cima).
// ===================================================================

loop @composicao (bars=62) {
    play @minhaAutoria;
}

// ===================================================================
// SONG — o roteiro da faixa.
// ===================================================================

song {
    play @composicao;
}
