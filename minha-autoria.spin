// ===================================================================
// minha-autoria.spin
// Transcrição da melodia original (solfejo) para SpinScript.
// Convenção usada na transcrição original: nota em MAIÚSCULA = oitava 5,
// minúscula = oitava 4; "#" indica sustenido; "." é pausa; "-" é ligadura
// (estende a nota anterior por mais um tempo).
// ===================================================================

@bpm = 100;

@piano = "https://cdn.spin.dev/instruments/grand_piano_c4.wav";

// ===================================================================
// MELODIA — cada nota tem: qual é, onde começa e quanto dura, em
// frações de compasso (beatsPerBar=4, então 1/4 = uma batida).
// ===================================================================

melody @minhaAutoria (instrument=@piano) {

    // ==== Tema ====
    // SOL FA SOL MI
    G5 0/4 1/4, F5 1/4 1/4, G5 2/4 1/4, E5 3/4 1/4,
    // SOL RE SOL
    G5 4/4 1/4, D5 5/4 1/4, G5 6/4 1/4,

    // ==== Parte A ====
    // SOL SOL MI FA SOL LA SOL FA MI RE
    G5 7/4 1/4, G5 8/4 1/4, E5 9/4 1/4, F5 10/4 1/4, G5 11/4 1/4, A5 12/4 1/4, G5 13/4 1/4, F5 14/4 1/4, E5 15/4 1/4, D5 16/4 1/4,
    // MI MI DO RE MI FA MI RE DO si
    E5 17/4 1/4, E5 18/4 1/4, C5 19/4 1/4, D5 20/4 1/4, E5 21/4 1/4, F5 22/4 1/4, E5 23/4 1/4, D5 24/4 1/4, C5 25/4 1/4, B4 26/4 1/4,
    // la la DO LA SOL DO
    A4 27/4 1/4, A4 28/4 1/4, C5 29/4 1/4, A5 30/4 1/4, G5 31/4 1/4, C5 32/4 1/4,
    // RE MI FA MI RE DO RE RE DO si
    D5 33/4 1/4, E5 34/4 1/4, F5 35/4 1/4, E5 36/4 1/4, D5 37/4 1/4, C5 38/4 1/4, D5 39/4 1/4, D5 40/4 1/4, C5 41/4 1/4, B4 42/4 1/4,

    // ==== Parte B ====
    // DO SOL# SOL FA RE# RE RE#
    C5 43/4 1/4, G#5 44/4 1/4, G5 45/4 1/4, F5 46/4 1/4, D#5 47/4 1/4, D5 48/4 1/4, D#5 49/4 1/4,
    // RE LA# SOL# SOL FA RE# FA MI
    D5 50/4 1/4, A#5 51/4 1/4, G#5 52/4 1/4, G5 53/4 1/4, F5 54/4 1/4, D#5 55/4 1/4, F5 56/4 1/4, E5 57/4 1/4,
    // sol fa sol mi sol
    G4 58/4 1/4, F4 59/4 1/4, G4 60/4 1/4, E4 61/4 1/4, G4 62/4 1/4,

    // ==== Parte C ====
    // DO DO DO DO DO
    C5 63/4 1/4, C5 64/4 1/4, C5 65/4 1/4, C5 66/4 1/4, C5 67/4 1/4,
    // la# sol# la# DO la#
    A#4 68/4 1/4, G#4 69/4 1/4, A#4 70/4 1/4, C5 71/4 1/4, A#4 72/4 1/4,
    // do# la# la# la# sol# sol# la# sol# sol sol# la# sol#
    C#4 73/4 1/4, A#4 74/4 1/4, A#4 75/4 1/4, A#4 76/4 1/4, G#4 77/4 1/4, G#4 78/4 1/4, A#4 79/4 1/4, G#4 80/4 1/4, G4 81/4 1/4, G#4 82/4 1/4, A#4 83/4 1/4, G#4 84/4 1/4,
    // do sol# sol# sol# sol sol fa sol fa fa fa fa fa fa re# fa sol
    C4 85/4 1/4, G#4 86/4 1/4, G#4 87/4 1/4, G#4 88/4 1/4, G4 89/4 1/4, G4 90/4 1/4, F4 91/4 1/4, G4 92/4 1/4, F4 93/4 1/4, F4 94/4 1/4, F4 95/4 1/4, F4 96/4 1/4, F4 97/4 1/4, F4 98/4 1/4, D#4 99/4 1/4, F4 100/4 1/4, G4 101/4 1/4,

    // ==== Parte D ====
    // sol# sol# la# DO
    G#4 102/4 1/4, G#4 103/4 1/4, A#4 104/4 1/4, C5 105/4 1/4,
    // DO DO DO . DO DO DO   (pausa de uma batida no meio)
    C5 106/4 1/4, C5 107/4 1/4, C5 108/4 1/4, C5 110/4 1/4, C5 111/4 1/4, C5 112/4 1/4,
    // la# sol# la# DO la#
    A#4 113/4 1/4, G#4 114/4 1/4, A#4 115/4 1/4, C5 116/4 1/4, A#4 117/4 1/4,

    // ==== Parte E ====
    // la# la# DO RE RE# RE RE# la# sol# sol# sol sol#
    A#4 118/4 1/4, A#4 119/4 1/4, C5 120/4 1/4, D5 121/4 1/4, D#5 122/4 1/4, D5 123/4 1/4, D#5 124/4 1/4, A#4 125/4 1/4, G#4 126/4 1/4, G#4 127/4 1/4, G4 128/4 1/4, G#4 129/4 1/4,
    // sol# la# sol# sol sol
    G#4 130/4 1/4, A#4 131/4 1/4, G#4 132/4 1/4, G4 133/4 1/4, G4 134/4 1/4,
    // sol sol SOL FA FA FA RE# RE RE#
    G4 135/4 1/4, G4 136/4 1/4, G5 137/4 1/4, F5 138/4 1/4, F5 139/4 1/4, F5 140/4 1/4, D#5 141/4 1/4, D5 142/4 1/4, D#5 143/4 1/4,

    // ==== Parte F ====
    // DO DO RE RE# RE#
    C5 144/4 1/4, C5 145/4 1/4, D5 146/4 1/4, D#5 147/4 1/4, D#5 148/4 1/4,
    // RE RE RE# – RE RE#   (a nota ligada dura duas batidas)
    D5 149/4 1/4, D5 150/4 1/4, D#5 151/4 2/4, D5 153/4 1/4, D#5 154/4 1/4,
    // DO FA RE# RE DO DO la#
    C5 155/4 1/4, F5 156/4 1/4, D#5 157/4 1/4, D5 158/4 1/4, C5 159/4 1/4, C5 160/4 1/4, A#4 161/4 1/4,
    // sol sol la# la# – sol sol la# la#   (a nota ligada dura duas batidas)
    G4 162/4 1/4, G4 163/4 1/4, A#4 164/4 1/4, A#4 165/4 2/4, G4 167/4 1/4, G4 168/4 1/4, A#4 169/4 1/4, A#4 170/4 1/4,
    // sol sol la# DO RE RE# RE RE# DO
    G4 171/4 1/4, G4 172/4 1/4, A#4 173/4 1/4, C5 174/4 1/4, D5 175/4 1/4, D#5 176/4 1/4, D5 177/4 1/4, D#5 178/4 1/4, C5 179/4 1/4,
    // RE# RE RE# DO DO SOL FA FA
    D#5 180/4 1/4, D5 181/4 1/4, D#5 182/4 1/4, C5 183/4 1/4, C5 184/4 1/4, G5 185/4 1/4, F5 186/4 1/4, F5 187/4 1/4,
    // FA FA RE# FA SOL
    F5 188/4 1/4, F5 189/4 1/4, D#5 190/4 1/4, F5 191/4 1/4, G5 192/4 1/4,

    // ==== Reprise do tema principal ====
    // SOL SOL MI FA SOL LA SOL FA MI RE
    G5 193/4 1/4, G5 194/4 1/4, E5 195/4 1/4, F5 196/4 1/4, G5 197/4 1/4, A5 198/4 1/4, G5 199/4 1/4, F5 200/4 1/4, E5 201/4 1/4, D5 202/4 1/4,
    // MI MI DO RE MI FA MI RE DO si
    E5 203/4 1/4, E5 204/4 1/4, C5 205/4 1/4, D5 206/4 1/4, E5 207/4 1/4, F5 208/4 1/4, E5 209/4 1/4, D5 210/4 1/4, C5 211/4 1/4, B4 212/4 1/4,
    // la la DO LA SOL DO
    A4 213/4 1/4, A4 214/4 1/4, C5 215/4 1/4, A5 216/4 1/4, G5 217/4 1/4, C5 218/4 1/4,
    // RE MI FA MI RE DO RE RE DO si
    D5 219/4 1/4, E5 220/4 1/4, F5 221/4 1/4, E5 222/4 1/4, D5 223/4 1/4, C5 224/4 1/4, D5 225/4 1/4, D5 226/4 1/4, C5 227/4 1/4, B4 228/4 1/4,

    // ==== Coda ====
    // do sol# sol fa re# re re#
    C4 229/4 1/4, G#4 230/4 1/4, G4 231/4 1/4, F4 232/4 1/4, D#4 233/4 1/4, D4 234/4 1/4, D#4 235/4 1/4,
    // re la# sol# sol fa re# fa
    D4 236/4 1/4, A#4 237/4 1/4, G#4 238/4 1/4, G4 239/4 1/4, F4 240/4 1/4, D#4 241/4 1/4, F4 242/4 1/4,
    // FA SOL FA MI
    F5 243/4 1/4, G5 244/4 1/4, F5 245/4 1/4, E5 246/4 1/4,
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
