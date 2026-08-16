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

    [Fact]
    public void RepeatingALoopAtSongLevelDoesNotAddExtraBarGaps()
    {
        // @bpm=120, beatsPerBar=4 (default) => 1 bar = 2000ms exactly.
        // `groove` has no `bars` param and its only statement is a bare
        // pattern play, so it should fall back to occupying exactly 1 bar.
        // `intro` repeats `groove` twice, so it should occupy exactly 4000ms
        // (2000ms per groove repeat), with no extra bar added on top.
        var input = """
@bpm = 120;
@kick = "kick.wav";

pattern @beat (sample=@kick, grid=16) {
    1
};

loop @groove {
    play @beat;
}

loop @intro {
    play @groove (repeat=2);
}

song {
    play @intro (repeat=2);
}
""";
        var interpreter = new Interpreter(input);
        interpreter.Interpret();

        var times = interpreter.interpretResult.Events.Select(e => e.Time).ToList();

        Assert.Equal([0, 2000, 4000, 6000], times);
    }

    [Fact]
    public void PatternAfterNestedLoopStillStacksAtTheLoopStart()
    {
        // @bpm=120, beatsPerBar=4 (default) => 1 bar = 2000ms exactly.
        // `intro` plays a nested loop (`kickLoop`, sequential — it advances
        // time by its own 1 bar) followed by a bare pattern (`pad`, parallel
        // — it must still land at intro's own start time, not wherever
        // kickLoop left _currentTime).
        var input = """
@bpm = 120;
@kick = "kick.wav";
@pad = "pad.wav";

pattern @kickBeat (sample=@kick, grid=16) {
    1
};

pattern @padBeat (sample=@pad, grid=16) {
    1
};

loop @kickLoop {
    play @kickBeat;
}

loop @intro {
    play @kickLoop;
    play @padBeat;
}

song {
    play @intro (repeat=2);
}
""";
        var interpreter = new Interpreter(input);
        interpreter.Interpret();

        var times = interpreter.interpretResult.Events
            .Select(e => (e.Sample, e.Time))
            .ToList();

        Assert.Equal(
            [("kick.wav", 0.0), ("pad.wav", 0.0), ("kick.wav", 2000.0), ("pad.wav", 2000.0)],
            times);
    }

    [Fact]
    public void RepeatOnABarePatternPlaysItSequentiallyTheRightNumberOfTimes()
    {
        // @bpm=120, beatsPerBar=4 (default) => 1 bar = 2000ms exactly.
        // `beat` (repeat=2) and `chimbal` (repeat=3) are parallel siblings:
        // both start at groove's own start time, each repeating on its own
        // bar-by-bar timeline. groove's own duration is dictated by the
        // longer one (chimbal, 3 bars).
        var input = """
@bpm = 120;
@kick = "kick.wav";
@hihat = "hihat.wav";

pattern @beat (sample=@kick, grid=16) {
    1
};

pattern @chimbal (sample=@hihat, grid=16) {
    1
};

loop @groove {
    play @beat (repeat=2);
    play @chimbal (repeat=3);
}

song {
    play @groove;
}
""";
        var interpreter = new Interpreter(input);
        interpreter.Interpret();

        var beatTimes = interpreter.interpretResult.Events
            .Where(e => e.Sample == "kick.wav")
            .Select(e => e.Time)
            .ToList();
        var chimbalTimes = interpreter.interpretResult.Events
            .Where(e => e.Sample == "hihat.wav")
            .Select(e => e.Time)
            .ToList();

        Assert.Equal([0.0, 2000.0], beatTimes);
        Assert.Equal([0.0, 2000.0, 4000.0], chimbalTimes);
    }
}
