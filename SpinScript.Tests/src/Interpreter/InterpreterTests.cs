namespace SpinScript.Tests.Intepreter;

using SpinScript.Interpreter;
using SpinScript.Parser;
using SpinScript.Lexer;
using Xunit;

public class InterpreterTests
{
    [Fact]
    public void TestInterpreterWithSimpleInput()
    {
        var input = """
@bpm = 129;

// ===================================================================
// SAMPLES (arquivos de áudio que a música vai usar)
// O interpretador deduz o que baixar a partir daqui.
// ===================================================================

@kick   = "https://d9olupt5igjta.cloudfront.net/samples/sample_files/804259/ef37106b83357433cc616296d69982a791a2fa10/mp3/_Kick_Sample.mp3";
@hihat  = "https://d9olupt5igjta.cloudfront.net/samples/sample_files/30265/c795c3a46852c3fc410f213ab03a4decac519b37/mp3/_hat_1.mp3";
// @piano  = "https://cdn.spin.dev/instruments/grand_piano_c4.wav";
// @vinil  = "https://cdn.spin.dev/loops/vinyl_crackle_bnaz.mp3";

// ===================================================================
// CATEGORIA A — percussão (sample disparado numa grade de steps)
// Sem altura, sem duração: o step liga, o sample toca.
// ===================================================================

pattern @beat (sample=@kick, grid=16) {
    1, 5, 9, 13, 15
};

pattern @chimbal (sample=@hihat, grid=16) {
    3, 7, 11, 15
};

// ===================================================================
// CATEGORIA B — melodia (notas com altura e duração, tempo livre)
// A nota tem: qual é (C4), quanto dura (1/4) e onde começa (offset).
// O som vem de um wav de piano, afinado por nota.
// ===================================================================

// pattern @melodia (instrument=@piano, free=false) {
    // nota  duracao  inicio
    // E4       1/4      0
    // G4       1/4      1/4
    // C5       1/2      1/2
    // B4       1/4      1
    // A4       1/4      5/4
    // G4       1/2      3/2
// };

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

loop @intro {
    play @groove (repeat=2); // a batida entra por cima
}

loop @verso {
    play @groove;
    // play @melodia;            // a melodia do piano entra no verso
}

// ===================================================================
// SONG — o roteiro da faixa
// ===================================================================

song {
    play @intro;
    // play @verso (repeat=2);
    // play @groove (repeat=4);
}
""";
        var interpreter = new Interpreter(input);
        interpreter.Interpret();

        Console.WriteLine($"{interpreter.interpretResult.Events.Count}");

        Console.WriteLine("**********");
        foreach (var soundEvent in interpreter.interpretResult.Events)
        {
            Console.WriteLine($"Sample: {soundEvent.Sample}, Time: {soundEvent.Time}, Velocity: {soundEvent.Velocity}");
        }
    }
}
