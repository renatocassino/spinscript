# SpinScript

SpinScript is a small language for describing music as code. You write the
drum pattern, the melody, and the arrangement of a track as text, and
SpinScript turns that into a timeline of playable audio events. The
inspiration is [OpenSCAD](https://openscad.org/): instead of drawing a 3D
shape with a mouse, you program it; instead of clicking notes onto a piano
roll, here you write them.

Source files use the `.spin` extension.

```spin
@bpm = 100;

beat @kick (sample=@kickSample, grid=16) { 1, 5, 9, 13 }

loop @groove (bars=1) {
    play @kick;
}

song {
    play @groove (repeat=4);
}
```

## Where this project came from

This started as a personal study project: *"what does it actually take to
build a lexer, a parser, and a compiler?"* The plan was small on purpose —
pick a toy domain, and build the full pipeline by hand instead of reading
about it.

The commit history tells that story pretty literally. It begins with a
hand-written tokenizer (`feat: finish lexer`), then a recursive-descent
parser producing an AST (`feat: add AST after parse`), then line/column
tracking for real error messages (`feat: add line and column to lexer`,
`feat: add lines to parser`), then a first walking interpreter
(`feat: start interpreter song`). Music was picked as the toy domain
because it has just enough structure — repetition, timing, nesting — to
make a lexer/parser/compiler exercise actually interesting, without the
scope of a general-purpose programming language.

From there it grew past "study project" on its own: a WebAssembly build so
the compiler can run in a browser tab (`feat: add wasm`), a playground web
app to actually hear the output (`feat: add first version of player with
spinscript`), and a VS Code extension for syntax highlighting
(`feat: add vscode extension`). What's below documents where all of that
landed.

## Table of contents

- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
- [Language reference](#language-reference)
  - [File basics](#file-basics)
  - [Variables](#variables)
  - [Values and parameters](#values-and-parameters)
  - [The three levels: beat/melody, loop, song](#the-three-levels-beatmelody-loop-song)
  - [Grid vs. bar](#grid-vs-bar)
  - [`beat`](#beat)
  - [`melody`](#melody)
  - [`loop`](#loop)
  - [`song`](#song)
  - [Comments](#comments)
  - [Errors and diagnostics](#errors-and-diagnostics)
  - [Grammar summary](#grammar-summary)
- [Full example](#full-example)
- [Glossary](#glossary)

---

## Repository layout

The repository is one language engine (`SpinScript.Core`) reused by four
different front ends: a test suite, a CLI, a browser build, and an editor
extension.

```
spinscript/
├── SpinScript.Core/       the language itself: lexer, parser, compiler
├── SpinScript.Tests/      xUnit test suite for Core
├── SpinScript/            command-line tool ("spinscript")
├── SpinScript.Wasm/       Core compiled to WebAssembly
├── spinscript-web/        browser playground (Astro + a Lit component)
├── spinscript-vscode/     VS Code extension (.spin syntax highlighting)
└── *.spin                 example/demo tracks at the repo root
```

### Pipeline

Every front end drives the same three-stage pipeline in `SpinScript.Core`:

```
 .spin source
     │
     ▼
 ┌─────────┐   List<Token>   ┌─────────┐   ProgramNode + errors   ┌──────────┐
 │  Lexer  │ ───────────────▶│ Parser  │ ────────────────────────▶│ Compiler │
 └─────────┘                 └─────────┘                          └──────────┘
                                                                        │
                                                             List<Event> (Sound/Melody)
                                                                        ▼
                                                        consumed by CLI / WASM+web player
```

- **Lexer** (`SpinScript.Core/Lexer/`) turns raw text into a flat
  `List<Token>`, tracking `Line`/`Column` (0-indexed internally, reported
  1-indexed in messages) for every token. It also owns note/pattern
  validation (`CheckIsNote`, pattern-grid detection) since those are
  lexical shapes, not grammar.
- **Parser** (`SpinScript.Core/Parser/`) is a hand-written recursive-descent
  parser. `Parser.Parse()` returns a `ParseResult(ProgramNode Ast,
  IReadOnlyList<ParserException> Errors)` — it does **not** throw on the
  first syntax error. Instead it records the error, resynchronizes to the
  next statement boundary, and keeps parsing, so a single call can report
  several independent mistakes in one pass, each with an accurate
  `Line`/`Column`.
- **Compiler** (`SpinScript.Core/Compiler/`) walks the AST and turns it into
  a flat `List<Event>` — `SoundEvent`s (a sample fired at a point in time)
  and `MelodyEvent`s (a sample pitch-shifted to a note, with a start and
  duration). It resolves `@variable` references, expands `loop`
  repetition, and converts every bar/beat/fraction into milliseconds using
  the song's BPM. It does not play audio itself — it just produces the
  timeline; that's the job of whatever front end embeds it.

### The front ends

| Project | What it is | How it uses Core |
|---|---|---|
| `SpinScript.Tests` | xUnit test suite (`TokenizerTests`, `ParserTests`, `CompilerTests`) | Calls `Lexer`/`Parser`/`Compiler` directly |
| `SpinScript` | CLI (`spinscript <file.spin>`), built on `Spectre.Console.Cli` | `RunCommand` parses a file and pretty-prints the AST as a tree (`WriteAst`); a syntax error is shown in a panel instead of a stack trace |
| `SpinScript.Wasm` | `SpinScript.Core` compiled to `browser-wasm` via .NET's WebAssembly SDK | Exposes two `[JSExport]` functions, `Parse(source)` and `Compile(source)`, each returning JSON (AST or event list, or `{error, line, column}` on failure) |
| `spinscript-web` | Astro site hosting a browser playground | The `<spinscript-editor>` Lit component loads the WASM build at runtime, does its own syntax highlighting (a regex mirroring the VS Code grammar), calls `Compile()`, and schedules the resulting events on the Web Audio API clock to actually play them |
| `spinscript-vscode` | VS Code extension | Registers the `.spin` language and a TextMate grammar (`syntaxes/spinscript.tmLanguage.json`) for syntax highlighting — no language server, no diagnostics from the real parser (yet) |

`SpinScript.Core` has no dependency on any of the others; everything else
depends on it. Editing the language means editing `SpinScript.Core`, and
every front end picks the change up for free once rebuilt — except
`spinscript-vscode`'s grammar, which is a separate, hand-maintained regex
approximation of the lexer and has to be updated by hand when token shapes
change.

## Getting started

Requires the .NET 10 SDK.

```bash
# run the whole test suite
dotnet test SpinScript.Tests

# run a .spin file (or inline source) through the CLI
dotnet run --project SpinScript -- song.spin
dotnet run --project SpinScript -- "@bpm = 120;"
```

To build the WebAssembly module and run the browser playground:

```bash
# one-time: install the wasm build tooling
sudo dotnet workload install wasm-tools

dotnet publish SpinScript.Wasm -c Release

cd spinscript-web
npm install
npm run dev   # runs `sync-wasm` first, copying the published build into public/wasm
```

To package the VS Code extension:

```bash
cd spinscript-vscode
npm install
npm run compile
npm run build:package   # produces spinscript-vscode-0.0.1.vsix
```

---

## Language reference

### File basics

A `.spin` file is a flat sequence of top-level statements: variable
assignments, `beat` declarations, `melody` declarations, `loop`
declarations, and exactly one `song` block. There's no explicit "main" —
the `song` block is the entry point the compiler looks for once everything
else has been declared.

```spin
@bpm = 120;

beat @kick (grid=16) { 1, 5, 9, 13 };

loop @groove (bars=1) {
    play @kick;
}

song {
    play @groove;
}
```

Order mostly doesn't matter for declarations (the compiler registers every
`beat`/`melody`/`loop`/assignment first, then walks the `song`), but a
file must have **exactly one** `song` block — a second one is a compile
error.

### Variables

Global configuration lives at the top of the file. A variable name starts
with `@`, is followed by a value, and ends with `;`:

```spin
@bpm = 100;
@kickSample = "https://cdn.example.com/kick.wav";
@muted = false;
```

`@` marks every named reference in the language, both where it's declared
and where it's used — seeing `@something` anywhere tells you it points at
something defined elsewhere in the file. Names start with a letter and may
contain letters, digits, and underscores (`@bpm`, `@main_groove`,
`@drop2`); `@2fast` and `@$x` are invalid.

Two variables the compiler reads by name from the top level: `@bpm`
(defaults to `120` if unset) and `@beatsPerBar` (defaults to `4`, i.e.
common 4/4 time). Everything else you assign — sample URLs, flags, notes
to self — is just a named value you can reference later with `@name`
wherever a parameter accepts one (most commonly `sample=@kickSample`).

### Values and parameters

Four value kinds exist anywhere a value is expected (a variable's value, a
parameter's value):

| Kind | Syntax | Example |
|---|---|---|
| Number | digits, optionally with a decimal point | `129`, `0.2` |
| String | single or double quotes (interchangeable, no escaping needed to mix them) | `"kick.wav"`, `'kick.wav'` |
| Boolean | the bare words | `true`, `false` |
| Reference | `@` followed by a name | `@kickSample` |

Parameter lists appear in parentheses after a `beat`/`melody`/`loop`/`song`
declaration or after a `play`, as a comma-separated list of `key=value`:

```spin
beat @kick (sample=@kickSample, grid=16) { 1, 5, 9, 13 }
play @groove (repeat=4);
```

A bare key with no `=value` (`beat @kick (free) { ... }`) is shorthand for
`free=true`. Writing the same key twice in one parameter list is a parse
error ("Parameter 'grid' was already set").

### The three levels: beat/melody, loop, song

The language has three levels. Understanding the hierarchy is
understanding the whole language.

**`beat`** and **`melody`** are the two ways to declare *what* makes
sound and *when*, within one bar. `beat` is percussion: a sample fires at
fixed grid positions, on or off, no pitch, no duration. `melody` is pitched
and free-timed: a sample gets pitch-shifted to a specific note, with its
own start time and duration expressed as a fraction of a bar, not tied to
any grid. Both are just declarations — nothing plays until something
`play`s them.

**`loop`** stacks `beat`/`melody`/other `loop`s and gives the result a
duration in bars. Everything played inside one `loop` at the same nesting
"slot" sounds at once — that's how a kick, a snare, and a hi-hat pattern
become one groove.

**`song`** sequences `loop`s (and bare patterns) along the timeline. It's
the track's script: play the intro, then the verse twice, then the chorus.
The macro structure of the piece lives here, and only here.

```
beat / melody  →  loop  →  song
 (what & when)   (stacked,  (sequenced,
  within 1 bar    N bars)    whole track)
```

### Grid vs. bar

Two different, perpendicular units — the distinction that trips people up
most.

**`grid`** is a `beat`'s resolution: how many steps its bar is sliced
into. `grid=16` means sixteen slots to place a hit in — the classic
sixteen-step sequencer row. Changing `grid` changes how fine the
subdivisions are, not how long the pattern lasts.

**`bars`** is a `loop`'s duration, in bars. A bar (measure) is a group of
`@beatsPerBar` beats (4 by default) — the "1, 2, 3, 4" you count before a
song starts. `bars=4` means the loop spans four bars. Changing `bars`
changes length, not resolution.

The common-case relationship between the two:

```
1 bar = 4 beats = 16 steps   (when grid = 16)
```

So with `grid=16`, each step is a quarter of a beat. Combined with BPM,
that becomes real time: at 120 BPM one beat is 0.5s, so one step is
0.125s.

When a `beat` is shorter than the `loop` it's played in, it repeats to
fill the space — a 1-bar beat played (bare, or via `play`) inside a
`bars=4` loop fires four times. That's how any drum machine works: a
one-bar groove loops to fill the section.

### `beat`

A `beat` names a sample and the step positions where it fires. Parameters
go in parentheses; the step list goes in braces.

```spin
beat @kick (sample=@kickSample, grid=16) { 1, 5, 9, 13 };
beat @hats (sample=@hihat,      grid=16) { 3, 7, 11, 15 };
```

Steps are 1-indexed: on a 16-step grid, step `1` is the very first slot of
the bar, step `9` lands exactly on the third beat. The `@hats` pattern
above hits the off-beats (3, 7, 11, 15) — the classic reggae skank against
a one-drop kick.

There's also a visual, drum-machine-row notation for a step list —
each character is one grid slot, `x` fires, `.` is silence, and `|` is a
purely cosmetic separator between beat groups:

```spin
beat @kick (sample=@kickSample, grid=16) { x...|.x..|..x.|x..x }
```

> Note: the grid notation's positions are counted from `0` (the first `x`
> is slot 0), while the comma-separated number list above is 1-indexed
> (`{ 9 }` means the ninth slot). The parser accepts both and produces a
> valid AST either way, but the compiler currently applies the same
> "subtract one" step-to-time conversion to both forms — so today, a grid
> pattern's hits land one step earlier than the equivalent number list
> would (and a hit on the very first slot lands *before* the bar even
> starts). Until that's reconciled, prefer the number-list form when you
> care about exact timing.

`grid` defaults to `16` if omitted. The trailing `;` after a `beat`'s `}`
is optional — both example lines above are valid with or without it.

### `melody`

A `melody` is a comma-separated list of notes: what pitch, when it starts,
and how long it lasts, all as fractions (or whole numbers) of a bar —
free timing, not grid-quantized.

```spin
melody @lead (sample=@piano) {
    E4 0 1/4, G4 1/4 1/4, C5 1/2 1/2, B4 1 1/4
};
```

Each note is `NOTE START DURATION`, optionally followed by its own
`(key=value, ...)` — that per-note parameter slot parses today but isn't
consumed by the compiler yet; it's reserved for future overrides like a
different sample or velocity per note. A trailing comma before the closing
`}` is allowed.

A note name is a letter `A`–`G`, optionally followed by `#` (sharp) or `b`
(flat), optionally followed by a single octave digit: `C4`, `G#5`, `Db`,
`A`, `F#9` are all valid. The `sample` you give a melody should be a
recording of a single pitch (the file names in this repo's examples spell
that out, e.g. `grand_piano_c4.wav`) — that pitch is the melody's
`rootNote` parameter, `"C4"` by default, and every note gets played back
at a playback-rate shift relative to it (so a `G4` note over a C4 sample
plays that same recording pitched up).

Start and duration accept either a plain integer (whole bars) or a
fraction like `1/4`. **As syntax sugar**, a note's start may instead be
written `+offset`, meaning "however many bars/fractions after the
*previous* note in this list ends":

```spin
melody @lead (sample=@piano) {
    G4 1/2 1/4, F4 +1/8 1/4
};
```

Here `F4` starts at `1/2 + 1/4` (where `G4` ends) `+ 1/8`. `+0` glues a
note directly onto the end of the previous one — which is the common case
for a run of notes with no rests, and is exactly what lets you edit an
earlier note's duration without hand-recalculating every absolute start
time below it. `+` can't be used on a list's first note (there's nothing
before it to be relative to) and always resolves to a plain absolute
fraction — the rest of the language, including the compiler, never sees
the `+` at all.

### `loop`

A `loop` stacks patterns and sets a duration in bars.

```spin
loop @groove (bars=1) {
    play @kick;
    play @snare;
    play @hats;
}
```

The three beats play stacked — that's how layers are built. There's no
special "play together" syntax; putting statements in the same loop *is*
"play together". A `loop` body can also declare a `beat`/`melody`/nested
`loop` inline instead of only referencing one via `play`, and can contain
plain `@name = value;` assignments.

`play` takes modifiers in parentheses, same shape as a declaration's
parameters:

```spin
play @groove (repeat=4);
```

`repeat` is the one modifier the compiler currently acts on — it plays the
target that many times in a row. (A bare pattern's repeats are laid out
one bar apart; a loop's repeats are laid out back-to-back over however
long each repetition of its body actually takes.)

**Parallel vs. sequential**, the one non-obvious timing rule: statements
that reference another `loop` (a nested `loop { }` or `play` of something
that's a `loop`) are *sequential* — each one starts where the previous
one left off. Everything else — a bare `beat`/`melody`, or `play` of a
`beat`/`melody` — is *parallel*: it always starts at the top of the
enclosing `loop`, regardless of what a preceding sibling statement did.
That's what makes this work:

```spin
loop @intro (bars=4) {
    play @ambientPad;         // parallel: starts at bar 0
    play @groove (repeat=4);  // parallel: also starts at bar 0, stacks on the pad
}
```

If `@groove` were itself a `loop` reference instead of a bare pattern, it
would instead advance the timeline for whatever comes after it in the
same body — that's what lets a `loop` be used purely as an arrangement
container (see [Full example](#full-example) below).

If a `loop` has no explicit `bars=`, its duration is inferred: the
furthest point any of its own statements reached (so `play`ing a 4-bar
loop inside a bar-less loop makes it 4 bars), or a single bar if nothing
inside advanced time on its own.

### `song`

`song` is the track's timeline. Its body may only contain `play`
statements and `@name = value;` assignments — no inline `beat`/`melody`/
`loop` declarations (declare those at the top level, then `play` them
here). Read top to bottom, it *is* the arrangement:

```spin
song {
    play @intro;
    play @verse (repeat=2);
    play @chorus;
    play @verse;
    play @chorus (repeat=2);
    play @outro;
}
```

A file needs exactly one `song` block — it's the compiler's entry point,
not just another declaration.

One grammar detail worth calling out because it's easy to trip on: `beat`
and `melody` bodies may optionally end with `;` after their closing `}` —
but `loop` and `song` bodies must **not** be followed by `;` at all
(that's a syntax error).

### Comments

Ignored by the lexer entirely; for humans only.

```spin
// a line comment, runs to the end of the line

/* a block comment,
   can span
   multiple lines */
```

### Errors and diagnostics

Both the lexer and the parser track `Line`/`Column` for everything (stored
0-indexed, reported 1-indexed in messages — `LexerException`/
`ParserException.Message` already includes `(line X, column Y)`).

The parser doesn't stop at the first mistake. `Parser.Parse()` returns a
`ParseResult` with a `ProgramNode` *and* a list of every `ParserException`
it hit along the way: on a syntax error it records the error, skips ahead
to the next statement boundary, and keeps going — so one call can report
several unrelated mistakes in a single pass, each pointing at its own
correct location, instead of stopping cold at the first typo.

### Grammar summary

What's legal where, at a glance:

| Context | Allowed statements |
|---|---|
| Top level (the file itself) | `@name = value;`, `beat`, `melody`, `loop`, exactly one `song` |
| Inside a `loop { }` | `@name = value;`, `beat`, `melody`, nested `loop`, `play` |
| Inside a `song { }` | `@name = value;`, `play` |
| `play` can target | a `beat`, a `melody`, or a `loop` — from inside either a `loop` or a `song` |

---

## Full example

An excerpt from [`minha-autoria.spin`](./minha-autoria.spin) (the repo's
largest example, an original melody transcribed end to end), showing
`melody`'s relative-start sugar doing real work — every note here just
glues onto the one before it, except where the transcription calls for an
actual rest:

```spin
melody @minhaAutoria (instrument=@piano) {

    // ==== Tema ====
    // SOL FA SOL MI
    G5 0/4 1/4, F5 +0 1/4, G5 +0 1/4, E5 +0 1/4,
    // SOL RE SOL
    G5 +0 1/4, D5 +0 1/4, G5 +0 1/4,

    // ...

    // ==== Parte D ====
    // DO DO DO . DO DO DO   (pausa de uma batida no meio)
    C5 +0 1/4, C5 +0 1/4, C5 +0 1/4, C5 +1/4 1/4, C5 +0 1/4, C5 +0 1/4,

    // ...
};
```

> Note: this file's melody parameter is `instrument=@piano`. The compiler
> currently only reads `sample`/`rootNote` off a `melody`'s parameters —
> `instrument` parses fine but isn't consumed yet, so new code should use
> `sample=@piano` instead.

And the arrangement that plays it, from the same file — a `loop` used
purely as a sequential container (no `bars=`, so its own length is just
"as long as its body takes"), and a `song` that plays that one loop:

```spin
loop @composicao (bars=62) {
    play @minhaAutoria;
}

song {
    play @composicao;
}
```

For a percussion-focused example, [`exemplo-completo.spin`](./exemplo-completo.spin)
walks through samples, a `beat`, a `melody`, and a layered arrangement in
one file; [`song.spin`](./song.spin) is a minimal one showing nested
`loop`s and `play` parameters.

---

## Glossary

- **step** — the smallest slot in a `beat`'s grid; where a hit can fire.
- **grid** — how many steps make up one bar of a `beat` (its resolution).
- **bar / measure** — `@beatsPerBar` beats; the unit a `loop`'s duration
  (`bars=`) is counted in.
- **beat (unit of time)** — the pulse; at a given BPM, one beat lasts
  `60 / BPM` seconds. Not to be confused with the `beat` *declaration*
  (a percussion pattern) — same word, two meanings, disambiguated by
  context throughout this document.
- **bpm** — beats per minute; sets how long a beat (and everything
  derived from it) lasts in real time.
- **`beat`** — a declaration: one sample plus the grid positions where it
  fires.
- **`melody`** — a declaration: a list of pitched, freely-timed notes
  played by one sample.
- **`loop`** — declarations and `play`s stacked together, with a duration
  in bars.
- **`song`** — the sequence of `play`s that forms the whole track; the
  compiler's entry point.
- **note** — one pitch, in `melody`: a letter `A`–`G`, optional `#`/`b`,
  optional octave digit.
- **fraction** — a time value written `n/d` (e.g. `1/4`), used for note
  start/duration.
- **reference** — an `@name`, either declaring a variable or pointing at
  one/at a `beat`/`melody`/`loop` elsewhere in the file.
- **token / AST / lexer / parser / compiler** — the usual compiler-theory
  terms; see [Repository layout](#repository-layout) for where each one
  lives in this codebase.
