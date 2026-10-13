namespace SpinScript.Tests.src.Compiler;

using SpinScript.Compiler;
using SpinScript.Parser;
using SpinScript.Lexer;
using Xunit;

public class CompilerTests
{
    [Fact]
    public void TestCompilerWithSimpleInput()
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
@piano = "https://cdn.freesound.org/previews/789/789144_24119-lq.mp3";
// @vinil  = "https://cdn.spin.dev/loops/vinyl_crackle_bnaz.mp3";

// ===================================================================
// CATEGORIA A — percussão (sample disparado numa grade de steps)
// Sem altura, sem duração: o step liga, o sample toca.
// ===================================================================

beat @beat (sample=@kick, grid=16) {
    1, 5, 9, 13, 15
};

beat @chimbal (sample=@hihat, grid=16) {
    3, 7, 11, 15
};

// ===================================================================
// CATEGORIA B — melodia (notas com altura e duração, tempo livre)
// A nota tem: qual é (C4), quanto dura (1/4) e onde começa (offset).
// O som vem de um wav de piano, afinado por nota.
// ===================================================================

melody @melodia (sample=@piano) {
    // nota     inicio   duracao
    E4       1/1      1/4,
    G4       1/4      1/4,
    C5       1/2      1/2,
    B4       1/4      1,
    A4       1/4      5/4,
    G4       1/2      3/2,
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

loop @intro {
    play @groove (repeat=2); // a batida entra por cima
    play @melodia;
}

loop @verso {
    play @groove;
    play @melodia;            // a melodia do piano entra no verso
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
        var compiler = new Compiler(input);
        compiler.Compile();

        Console.WriteLine($"{compiler.compileResult.Events.Count}");

        Console.WriteLine("**********");
        foreach (var soundEvent in compiler.compileResult.Events.OfType<SoundEvent>())
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

beat @beat (sample=@kick, grid=16) {
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
        var compiler = new Compiler(input);
        compiler.Compile();

        var times = compiler.compileResult.Events.OfType<SoundEvent>().Select(e => e.Time).ToList();

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

beat @kickBeat (sample=@kick, grid=16) {
    1
};

beat @padBeat (sample=@pad, grid=16) {
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
        var compiler = new Compiler(input);
        compiler.Compile();

        var times = compiler.compileResult.Events
            .OfType<SoundEvent>()
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

beat @beat (sample=@kick, grid=16) {
    1
};

beat @chimbal (sample=@hihat, grid=16) {
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
        var compiler = new Compiler(input);
        compiler.Compile();

        var beatTimes = compiler.compileResult.Events
            .OfType<SoundEvent>()
            .Where(e => e.Sample == "kick.wav")
            .Select(e => e.Time)
            .ToList();
        var chimbalTimes = compiler.compileResult.Events
            .OfType<SoundEvent>()
            .Where(e => e.Sample == "hihat.wav")
            .Select(e => e.Time)
            .ToList();

        Assert.Equal([0.0, 2000.0], beatTimes);
        Assert.Equal([0.0, 2000.0, 4000.0], chimbalTimes);
    }

    [Fact]
    public void PatternGridAndNumberListProduceTheSameTimes()
    {
        // @bpm=120, grid=16 => 1 step = 125ms. O primeiro 'x' do grid é o
        // step 1 (0ms), não um step antes do compasso.
        var input = """
@bpm = 120;
@kick = "kick.wav";
@hihat = "hihat.wav";

beat @grid (sample=@kick, grid=16) { x...|x...|..x.|...x };
beat @list (sample=@hihat, grid=16) { 1, 5, 11, 16 };

loop @groove {
    play @grid;
    play @list;
}

song {
    play @groove;
}
""";
        var compiler = new Compiler(input);
        compiler.Compile();

        var gridTimes = compiler.compileResult.Events
            .OfType<SoundEvent>()
            .Where(e => e.Sample == "kick.wav")
            .Select(e => e.Time)
            .ToList();
        var listTimes = compiler.compileResult.Events
            .OfType<SoundEvent>()
            .Where(e => e.Sample == "hihat.wav")
            .Select(e => e.Time)
            .ToList();

        Assert.Equal([0.0, 500.0, 1250.0, 1875.0], gridTimes);
        Assert.Equal(listTimes, gridTimes);
    }

    [Fact]
    public void MelodyStartsWhereItIsPlayedAndKeepsItsOwnDuration()
    {
        // @bpm=120 => 1 bar = 2000ms. @verse comes after a 1-bar @intro,
        // so its notes are offset by one bar.
        var input = """
@bpm = 120;
@kick = "kick.wav";
@piano = "piano.wav";

beat @beat (sample=@kick) { 1 };
melody @lead (sample=@piano) { C4 0 1/4, E4 1/2 1/4 };

loop @intro (bars=1) { play @beat; }
loop @verse (bars=1) { play @lead; }

song {
    play @intro;
    play @verse;
}
""";
        var compiler = new Compiler(input);
        compiler.Compile();

        var notes = compiler.compileResult.Events.OfType<MelodyEvent>().ToList();

        Assert.Equal([2000.0, 3000.0], notes.Select(n => n.Time));
        Assert.Equal([500.0, 500.0], notes.Select(n => n.Duration));
    }

    [Fact]
    public void ChordNotesStartTogetherWithTheirOwnPitch()
    {
        var input = """
@bpm = 120;
@piano = "piano.wav";

melody @chords (sample=@piano) { C4:maj 0 1/2, [A3 C4] +0 1/2 };

song {
    play @chords;
}
""";
        var compiler = new Compiler(input);
        compiler.Compile();

        var notes = compiler.compileResult.Events.OfType<MelodyEvent>().ToList();

        Assert.Equal([0.0, 0.0, 0.0, 1000.0, 1000.0], notes.Select(n => n.Time));
        Assert.Equal(["C4", "E4", "G4", "A3", "C4"], notes.Select(n => n.Note));
        // rate relativo ao rootNote padrão (C4): E4 = 4 semitons acima, A3 = 3 abaixo
        Assert.Equal(Math.Pow(2, 4 / 12.0), notes[1].Rate, 6);
        Assert.Equal(Math.Pow(2, -3 / 12.0), notes[3].Rate, 6);
    }

    [Fact]
    public void RestLeavesSilenceBetweenNotes()
    {
        // @bpm=120 => 1 bar = 2000ms, 1/4 = 500ms
        var input = """
@bpm = 120;
@piano = "piano.wav";

melody @lead (sample=@piano) { C5 0 1/4, _ 1/4, D5 +0 1/4 };

song {
    play @lead;
}
""";
        var compiler = new Compiler(input);
        compiler.Compile();

        var notes = compiler.compileResult.Events.OfType<MelodyEvent>().ToList();
        Assert.Equal([0.0, 1000.0], notes.Select(n => n.Time));
        Assert.Equal(["C5", "D5"], notes.Select(n => n.Note));
    }

    private const string Parts = """
@bpm = 120;
@kick = "kick.wav";
@hat = "hat.wav";
@piano = "piano.wav";

beat @kicks (sample=@kick) { 1, 9 }
beat @hats (sample=@hat) { 3, 7, 11, 15 }
melody @lead (sample=@piano) { C4 0 1/4 };

loop @groove { play @kicks; play @hats; }

song {
    play @lead;
    play @groove (repeat=4);
}
""";

    [Fact]
    public void CompilePartPlaysOnlyThatPartFromTheStart()
    {
        var result = new Compiler(Parts).CompilePart("hats");

        var events = result.Events.Cast<SoundEvent>().ToList();
        Assert.All(events, e => Assert.Equal("hat.wav", e.Sample));
        Assert.Equal([250.0, 750.0, 1250.0, 1750.0], events.Select(e => e.Time));
    }

    [Fact]
    public void CompilePartOfALoopIncludesWhatItPlays()
    {
        var result = new Compiler(Parts).CompilePart("groove");

        Assert.Equal(["hat.wav", "kick.wav"], result.Events.Cast<SoundEvent>().Select(e => e.Sample).Distinct().Order());
        Assert.Equal(0.0, result.Events.Cast<SoundEvent>().Min(e => e.Time));
    }

    [Fact]
    public void CompilePartIgnoresErrorsInTheSongAndInPartsItDoesntPlay()
    {
        var broken = Parts
            .Replace("play @lead;", "play @doesNotExist;")
            .Replace("melody @lead (sample=@piano)", "melody @lead (sample=@nope)");

        Assert.Throws<CompilerException>(() => new Compiler(broken).Compile());

        var result = new Compiler(broken).CompilePart("groove");
        Assert.NotEmpty(result.Events);
    }

    [Fact]
    public void CompilePartStillReportsErrorsInsideThePart()
    {
        var broken = Parts.Replace("{ 3, 7, 11, 15 }", "{ 3, 99 }");

        var exception = Assert.Throws<CompilerException>(() => new Compiler(broken).CompilePart("groove"));
        Assert.Contains(exception.Errors, e => e.Code == "step-out-of-grid");
    }

    [Fact]
    public void CompilePartOfAnUnknownNameIsAnError()
    {
        var exception = Assert.Throws<CompilerException>(() => new Compiler(Parts).CompilePart("nope"));
        Assert.Contains("'@nope'", exception.Message);
    }

    [Fact]
    public void LengthIsTheTimelineEndNotTheLastEvent()
    {
        // 120 BPM: 1 bar = 2000ms. The last hit is at 1500ms, but the part
        // (and a song playing it twice) lasts whole bars.
        var source = Parts.Replace("{ 3, 7, 11, 15 }", "{ 13 }");

        Assert.Equal(2000.0, new Compiler(source).CompilePart("hats").Length);
        Assert.Equal(2000.0, new Compiler(source).CompilePart("groove").Length);
        Assert.Equal(2000.0 + 4 * 2000.0, new Compiler(source).Compile().Length);
    }

    private const string Transposed = """
@bpm = 120;
@piano = "piano.wav";
@kick = "kick.wav";

melody @lead (sample=@piano) { C4 0 1/4, [C4 E4] 1/4 1/4 };
beat @kicks (sample=@kick) { 1 }

loop @verse { play @lead (transpose=12); play @kicks; }

song {
    PLAYS
}
""";

    private static List<MelodyEvent> Notes(string plays) =>
        new Compiler(Transposed.Replace("PLAYS", plays)).Compile().Events.OfType<MelodyEvent>().ToList();

    [Fact]
    public void TransposeShiftsNotesBySemitones()
    {
        var notes = Notes("play @lead (transpose=-2);");

        Assert.Equal(["A#3", "A#3", "D4"], notes.Select(n => n.Note));
        Assert.Equal(Math.Pow(2, -2 / 12.0), notes[0].Rate, 6); // relativo ao rootNote C4
    }

    [Fact]
    public void NestedTransposesAddUp()
    {
        var notes = Notes("play @verse (transpose=2);");

        Assert.Equal(["D5", "D5", "F#5"], notes.Select(n => n.Note));
    }

    [Fact]
    public void TransposeOnlyAppliesInsideItsPlay()
    {
        var notes = Notes("play @lead (transpose=12); play @lead;");

        Assert.Equal(["C5", "C5", "E5", "C4", "C4", "E4"], notes.Select(n => n.Note));
    }

    [Fact]
    public void TransposeDoesNotChangeBeats()
    {
        var withTranspose = new Compiler(Transposed.Replace("PLAYS", "play @verse (transpose=5);")).Compile();
        var without = new Compiler(Transposed.Replace("PLAYS", "play @verse;")).Compile();

        Assert.Equal(without.Events.OfType<SoundEvent>(), withTranspose.Events.OfType<SoundEvent>());
    }

    private const string Parallel = """
@bpm = 120;
@piano = "piano.wav";
@kick = "kick.wav";

melody @chords (sample=@piano) { C4 0 2 };
beat @kicks (sample=@kick) { 1 }
loop @drums { play @kicks; }

song {
    PLAYS
}
""";

    private static CompileResult CompileParallel(string plays) => new Compiler(Parallel.Replace("PLAYS", plays)).Compile();

    [Fact]
    public void ParallelMembersStartTogetherEvenWhenOneIsALoop()
    {
        // Without '&', a loop after a melody would wait for it to end.
        var result = CompileParallel("play @chords & @drums;");

        Assert.Equal(0.0, result.Events.OfType<SoundEvent>().Single().Time);
        Assert.Equal(0.0, result.Events.OfType<MelodyEvent>().Single().Time);
        Assert.Equal(4000.0, result.Length); // the longest member: 2 bars
    }

    [Fact]
    public void ParallelRepeatRepeatsTheWholeGroupAfterItsLongestMember()
    {
        var result = CompileParallel("play @chords & @kicks (repeat=2); play @kicks;");

        Assert.Equal([0.0, 4000.0, 8000.0], result.Events.OfType<SoundEvent>().Select(e => e.Time));
        Assert.Equal([0.0, 4000.0], result.Events.OfType<MelodyEvent>().Select(e => e.Time));
    }

    [Fact]
    public void ParallelTransposeAppliesToEveryMember()
    {
        var result = CompileParallel("play @chords & @drums (transpose=12);");

        Assert.Equal("C5", result.Events.OfType<MelodyEvent>().Single().Note);
    }

    [Fact]
    public void CompilePartFollowsParallelPlays()
    {
        var source = Parallel.Replace("PLAYS", "play @drums;").Replace("loop @drums { play @kicks; }", "loop @drums { play @kicks & @chords; }");

        var result = new Compiler(source).CompilePart("drums");

        Assert.Single(result.Events.OfType<MelodyEvent>());
    }
}
