import { LitElement, css, html, type TemplateResult } from 'lit';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/copy-button/copy-button.js';

import { highlightSpinScript, tokenStyles } from '../lib/highlight';
import { sampleGroups } from '../lib/samples';

const KICK = '/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Punchy.wav';
const SNARE = '/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Tight.wav';
const HAT = '/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 01.wav';
const PIANO = '/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Piano (C3).wav';

const SECTIONS: { id: string; title: string }[] = [
  { id: 'intro', title: 'Introduction' },
  { id: 'basics', title: 'File basics' },
  { id: 'variables', title: 'Variables & values' },
  { id: 'beat', title: 'beat' },
  { id: 'melody', title: 'melody' },
  { id: 'chords', title: 'Chords' },
  { id: 'rests', title: 'Rests' },
  { id: 'loop', title: 'loop' },
  { id: 'song', title: 'song' },
  { id: 'timing', title: 'Timing' },
  { id: 'sounds', title: 'Sounds' },
  { id: 'diagnostics', title: 'Errors & warnings' },
  { id: 'playground', title: 'Using the playground' },
  { id: 'cheatsheet', title: 'Cheat sheet' },
];

/**
 * The language reference shown in the playground's docs sheet. Code blocks
 * marked runnable get a "Try it" button that fires a `try-code` event
 * (detail: { title, code }) so the editor can open them in a new tab.
 */
export class SpinScriptDocs extends LitElement {
  static styles = [
    tokenStyles,
    css`
      :host {
        display: grid;
        grid-template-columns: 200px minmax(0, 1fr);
        gap: var(--wa-space-xl);
        font-family: var(--wa-font-family-body);
        line-height: 1.6;
        color: var(--wa-color-text-normal);
      }

      nav {
        position: sticky;
        top: 0;
        align-self: start;
        display: flex;
        flex-direction: column;
        gap: var(--wa-space-3xs);
        font-size: var(--wa-font-size-s);
      }

      nav a {
        color: var(--wa-color-text-quiet);
        text-decoration: none;
        padding: var(--wa-space-3xs) var(--wa-space-xs);
        border-radius: var(--wa-border-radius-s);
      }

      nav a:hover {
        color: var(--wa-color-text-normal);
        background: var(--wa-color-surface-lowered);
      }

      article {
        max-width: 760px;
        min-width: 0;
      }

      section {
        scroll-margin-top: var(--wa-space-m);
        padding-bottom: var(--wa-space-l);
      }

      h2 {
        font-family: var(--wa-font-family-heading);
        font-size: var(--wa-font-size-xl);
        margin: var(--wa-space-l) 0 var(--wa-space-s);
      }

      section:first-child h2 {
        margin-top: 0;
      }

      h3 {
        font-size: var(--wa-font-size-m);
        margin: var(--wa-space-l) 0 var(--wa-space-xs);
      }

      p,
      li {
        max-width: 68ch;
      }

      kbd {
        font-family: var(--wa-font-family-code);
        font-size: 0.85em;
        padding: 0 0.3em;
        border: 1px solid var(--wa-color-surface-border);
        border-radius: var(--wa-border-radius-s);
      }

      code {
        font-family: var(--wa-font-family-code);
        font-size: 0.9em;
        background: var(--wa-color-surface-lowered);
        padding: 0.1em 0.35em;
        border-radius: var(--wa-border-radius-s);
      }

      .example {
        position: relative;
        margin: var(--wa-space-s) 0 var(--wa-space-m);
        border: 1px solid var(--wa-color-surface-border);
        border-radius: var(--wa-border-radius-m);
        background: var(--wa-color-surface-default);
      }

      .example pre {
        margin: 0;
        padding: var(--wa-space-s) var(--wa-space-m);
        overflow-x: auto;
        font-family: var(--wa-font-family-code);
        font-size: 13px;
        line-height: 1.5;
      }

      .example pre code {
        background: none;
        padding: 0;
        font-size: inherit;
      }

      .example .try {
        position: absolute;
        top: var(--wa-space-2xs);
        right: var(--wa-space-2xs);
      }

      table {
        border-collapse: collapse;
        margin: var(--wa-space-s) 0 var(--wa-space-m);
        font-size: var(--wa-font-size-s);
        width: 100%;
      }

      th,
      td {
        text-align: left;
        vertical-align: top;
        padding: var(--wa-space-2xs) var(--wa-space-s);
        border-bottom: 1px solid var(--wa-color-surface-border);
      }

      th {
        color: var(--wa-color-text-quiet);
        font-weight: var(--wa-font-weight-semibold);
      }

      .note {
        padding: var(--wa-space-s) var(--wa-space-m);
        border-left: 3px solid var(--wa-color-brand-fill-loud);
        background: var(--wa-color-surface-lowered);
        border-radius: var(--wa-border-radius-s);
        margin: var(--wa-space-s) 0 var(--wa-space-m);
      }

      .sounds {
        display: grid;
        gap: var(--wa-space-m);
      }

      .sound {
        display: flex;
        align-items: center;
        gap: var(--wa-space-xs);
        font-size: var(--wa-font-size-s);
        padding: var(--wa-space-3xs) 0;
      }

      .sound .label {
        min-width: 9em;
      }

      .sound code {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        flex: 1;
        min-width: 0;
      }

      @media (max-width: 720px) {
        :host {
          grid-template-columns: minmax(0, 1fr);
        }

        nav {
          position: static;
          flex-direction: row;
          flex-wrap: wrap;
        }
      }
    `,
  ];

  #scrollTo(event: Event, id: string): void {
    event.preventDefault();
    this.shadowRoot!.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  #try(title: string, code: string): void {
    this.dispatchEvent(new CustomEvent('try-code', { detail: { title, code }, bubbles: true, composed: true }));
  }

  /** A highlighted code block; pass `tryTitle` when it's a complete, runnable song. */
  #code(code: string, tryTitle?: string): TemplateResult {
    const source = code.trim() + '\n';
    return html`<div class="example">
      ${tryTitle
        ? html`<wa-button class="try" size="small" appearance="outlined" @click=${() => this.#try(tryTitle, source)}
            >Try it</wa-button
          >`
        : null}
      <pre><code>${unsafeHTML(highlightSpinScript(source).trimEnd())}</code></pre>
    </div>`;
  }

  override render() {
    return html`
      <nav aria-label="Documentation sections">
        ${SECTIONS.map((s) => html`<a href="#${s.id}" @click=${(e: Event) => this.#scrollTo(e, s.id)}>${s.title}</a>`)}
      </nav>
      <article>
        ${this.#intro()} ${this.#basics()} ${this.#variables()} ${this.#beat()} ${this.#melody()} ${this.#chords()}
        ${this.#rests()} ${this.#loop()} ${this.#song()} ${this.#timing()} ${this.#sounds()} ${this.#diagnostics()}
        ${this.#playground()} ${this.#cheatsheet()}
      </article>
    `;
  }

  #intro() {
    return html`<section id="intro">
      <h2>Introduction</h2>
      <p>
        SpinScript is a small language for describing music as code. You write the drum pattern, the melody and the
        arrangement of a track as text, and SpinScript turns it into a timeline of sounds — like writing a 3D model
        in OpenSCAD instead of drawing it, or notes in a file instead of clicking them into a piano roll.
      </p>
      <p>A whole song in eleven lines:</p>
      ${this.#code(
        `
@bpm = 100;
@kick = "${KICK}";
@snare = "${SNARE}";

beat @kickPart  (sample=@kick)  { 1, 5, 9, 13 }
beat @snarePart (sample=@snare) { 5, 13 }

loop @groove { play @kickPart; play @snarePart; }

song {
    play @groove (repeat=4);
}`,
        'Hello, groove',
      )}
      <p>The language has three levels, and understanding them is understanding the whole language:</p>
      <table>
        <tr>
          <th>Level</th>
          <th>What it is</th>
        </tr>
        <tr>
          <td><code>beat</code> / <code>melody</code></td>
          <td>
            <em>What</em> makes a sound and <em>when</em>, within a bar. A beat fires a sample on grid steps (drums);
            a melody plays pitched notes with their own start and length.
          </td>
        </tr>
        <tr>
          <td><code>loop</code></td>
          <td>Stacks beats, melodies and other loops into a section, N bars long.</td>
        </tr>
        <tr>
          <td><code>song</code></td>
          <td>The timeline: plays sections one after the other. Every file has exactly one.</td>
        </tr>
      </table>
      <p>Nothing makes a sound until something <code>play</code>s it, and nothing plays unless the <code>song</code> reaches it.</p>
    </section>`;
  }

  #basics() {
    return html`<section id="basics">
      <h2>File basics</h2>
      <p>
        A file is a flat list of statements: variable assignments, <code>beat</code>, <code>melody</code> and
        <code>loop</code> declarations, and one <code>song</code> block. Declaration order doesn't matter — you can
        play something declared further down.
      </p>
      ${this.#code(`
// a line comment

/* a block comment,
   over several lines */`)}
      <p>
        Assignments end with <code>;</code>. A <code>beat</code> or <code>melody</code> may optionally end with
        <code>;</code> after its <code>}</code>; a <code>loop</code> or <code>song</code> must not.
      </p>
    </section>`;
  }

  #variables() {
    return html`<section id="variables">
      <h2>Variables & values</h2>
      <p>Variables hold the settings and sample URLs a song uses. Their names start with <code>@</code>:</p>
      ${this.#code(`
@bpm = 120;
@kick = "${KICK}";
@muted = false;`)}
      <p>
        <code>@</code> marks every name in the language, where it's declared and where it's used — <code>@kick</code>
        anywhere means "the thing called kick". Names start with a letter and may contain letters, digits and
        underscores.
      </p>
      <h3>Values</h3>
      <table>
        <tr>
          <th>Kind</th>
          <th>Examples</th>
        </tr>
        <tr>
          <td>Number</td>
          <td><code>120</code>, <code>0.5</code></td>
        </tr>
        <tr>
          <td>String</td>
          <td><code>"kick.wav"</code> or <code>'kick.wav'</code></td>
        </tr>
        <tr>
          <td>Boolean</td>
          <td><code>true</code>, <code>false</code></td>
        </tr>
        <tr>
          <td>Reference</td>
          <td><code>@kick</code> — in a parameter, uses that variable's value</td>
        </tr>
      </table>
      <h3>Settings the song reads</h3>
      <table>
        <tr>
          <th>Variable</th>
          <th>Default</th>
          <th>Meaning</th>
        </tr>
        <tr>
          <td><code>@bpm</code></td>
          <td><code>120</code></td>
          <td>Beats per minute. Fractions are fine (<code>127.5</code>).</td>
        </tr>
        <tr>
          <td><code>@beatsPerBar</code></td>
          <td><code>4</code></td>
          <td>How many beats make a bar (4 = common 4/4 time). A whole number.</td>
        </tr>
      </table>
      <h3>Parameters</h3>
      <p>
        Declarations and <code>play</code> take parameters in parentheses, as <code>key=value</code> pairs. A bare
        key is shorthand for <code>key=true</code>. Each key may appear once.
      </p>
      ${this.#code(`
beat @kickPart (sample=@kick, grid=16) { 1, 9 }
play @groove (repeat=4);`)}
    </section>`;
  }

  #beat() {
    return html`<section id="beat">
      <h2>beat</h2>
      <p>
        A <code>beat</code> is percussion: one sample, fired at fixed positions of a grid. No pitch, no length — the
        step turns on, the sample plays.
      </p>
      ${this.#code(`
beat @kickPart (sample=@kick, grid=16) { 1, 5, 9, 13 }`)}
      <table>
        <tr>
          <th>Parameter</th>
          <th>Default</th>
          <th>Meaning</th>
        </tr>
        <tr>
          <td><code>sample</code></td>
          <td>required</td>
          <td>The sound to play: a URL, usually through a variable.</td>
        </tr>
        <tr>
          <td><code>grid</code></td>
          <td><code>16</code></td>
          <td>How many steps one bar is split into. A whole number, at least 1.</td>
        </tr>
      </table>
      <p>
        Steps are counted from <strong>1</strong>. With <code>grid=16</code>, steps 1, 5, 9 and 13 are the four beats
        of the bar, and 3, 7, 11, 15 the off-beats in between. A step outside the grid (<code>{ 17 }</code> on a
        16-step grid) is an error.
      </p>
      <h3>Drum-machine rows</h3>
      <p>
        Instead of numbers you can draw the row: each character is one step — <code>x</code> plays, <code>.</code>
        (or <code>-</code>) is silence, and <code>|</code> is just a visual divider that doesn't take a step.
      </p>
      ${this.#code(
        `
@kick = "${KICK}";
@hat = "${HAT}";

beat @kickPart (sample=@kick) { x...|..x.|x...|.x.. }
beat @hatPart  (sample=@hat)  { x.x.|x.x.|x.x.|x.xx }

loop @groove { play @kickPart; play @hatPart; }

song { play @groove (repeat=4); }`,
        'Drum rows',
      )}
      <p>
        Both forms mean exactly the same thing: <code>{ x...|x... }</code> is <code>{ 1, 5 }</code>. A beat always
        lasts one bar; played in a longer loop, repeat it (<code>play @kickPart (repeat=4);</code>).
      </p>
    </section>`;
  }

  #melody() {
    return html`<section id="melody">
      <h2>melody</h2>
      <p>
        A <code>melody</code> plays pitched notes from one sample. Each note says which note, when it starts, and how
        long it lasts:
      </p>
      ${this.#code(
        `
@piano = "${PIANO}";

melody @lead (sample=@piano, rootNote="C3") {
    // NOTE  START  LENGTH
    E4  0    1/4,
    G4  1/4  1/4,
    C5  1/2  1/2,
};

song { play @lead (repeat=2); }`,
        'First melody',
      )}
      <table>
        <tr>
          <th>Parameter</th>
          <th>Default</th>
          <th>Meaning</th>
        </tr>
        <tr>
          <td><code>sample</code></td>
          <td>required</td>
          <td>A recording of a single note. <code>instrument</code> works as an alias.</td>
        </tr>
        <tr>
          <td><code>rootNote</code></td>
          <td><code>"C4"</code></td>
          <td>
            The note the sample was recorded at. Every note is played back faster or slower relative to it, so this
            must match the recording (the playground's tonal sounds are all <code>C3</code>, the 808s <code>A1</code>).
          </td>
        </tr>
      </table>
      <h3>Notes</h3>
      <p>
        A note is a letter <code>A</code>–<code>G</code>, an optional <code>#</code> (sharp) or <code>b</code> (flat),
        and an optional octave digit: <code>C4</code>, <code>F#3</code>, <code>Bb5</code>. Without an octave a note is
        in octave 4, so <code>A</code> is <code>A4</code>. Middle C is <code>C4</code>.
      </p>
      <h3>Start and length</h3>
      <p>
        Both are measured in <strong>bars</strong>, as whole numbers or fractions: <code>0</code> is the top of the
        melody's first bar, <code>1/4</code> one beat later (in 4/4), <code>1</code> the top of the second bar.
        Notes don't have to be in order, and they may overlap.
      </p>
      <h3>Relative starts with <code>+</code></h3>
      <p>
        Writing the start as <code>+offset</code> means "this long after the previous note ends". <code>+0</code>
        glues a note right onto the previous one — and when you later change an earlier note's length, everything
        after it moves along by itself.
      </p>
      ${this.#code(
        `
@piano = "${PIANO}";

melody @scale (sample=@piano, rootNote="C3") {
    C4 0 1/8, D4 +0 1/8, E4 +0 1/8, F4 +0 1/8,
    G4 +0 1/8, A4 +0 1/8, B4 +0 1/8, C5 +0 1/8,
    C5 +1/8 1/4,     // +1/8: a short gap first
};

song { play @scale; }`,
        'Scale',
      )}
      <p>
        A melody takes up as many whole bars as it needs, so playing it twice (<code>repeat=2</code>) starts the
        second time on the next bar line after the first one ends.
      </p>
    </section>`;
  }

  #chords() {
    return html`<section id="chords">
      <h2>Chords</h2>
      <p>
        Anywhere a note goes, a chord can go instead — several notes sharing the same start and length. List the
        notes in brackets, or name the chord as <code>ROOT:quality</code>:
      </p>
      ${this.#code(
        `
@piano = "${PIANO}";

melody @progression (sample=@piano, rootNote="C3") {
    [C4 E4 G4]  0  1,    // spelled out (commas optional)
    G3:maj      1  1,    // G B D
    A3:m        2  1,    // A C E
    F3:maj7     3  1,    // F A C E
};

song { play @progression; }`,
        'Chords',
      )}
      <p>
        The <code>:</code> tells a chord apart from a note: <code>G7</code> is the note G in octave 7,
        <code>G3:7</code> is a G seventh chord. Chords are stacked upward from the root, whose octave is optional
        like a note's. Quality names are case-sensitive (<code>m</code> is minor).
      </p>
      <table>
        <tr>
          <th>Quality</th>
          <th>Chord</th>
          <th>Quality</th>
          <th>Chord</th>
        </tr>
        <tr>
          <td><code>maj</code></td>
          <td>major</td>
          <td><code>7</code></td>
          <td>dominant seventh</td>
        </tr>
        <tr>
          <td><code>m</code>, <code>min</code></td>
          <td>minor</td>
          <td><code>maj7</code></td>
          <td>major seventh</td>
        </tr>
        <tr>
          <td><code>dim</code></td>
          <td>diminished</td>
          <td><code>m7</code></td>
          <td>minor seventh</td>
        </tr>
        <tr>
          <td><code>aug</code></td>
          <td>augmented</td>
          <td><code>mmaj7</code></td>
          <td>minor-major seventh</td>
        </tr>
        <tr>
          <td><code>sus2</code>, <code>sus4</code></td>
          <td>suspended</td>
          <td><code>dim7</code>, <code>m7b5</code></td>
          <td>diminished / half-diminished seventh</td>
        </tr>
        <tr>
          <td><code>5</code></td>
          <td>power chord</td>
          <td><code>aug7</code>, <code>7sus4</code></td>
          <td>augmented / suspended seventh</td>
        </tr>
        <tr>
          <td><code>6</code>, <code>m6</code></td>
          <td>sixths</td>
          <td><code>add9</code>, <code>9</code>, <code>maj9</code>, <code>m9</code></td>
          <td>ninths</td>
        </tr>
      </table>
      <p>A <code>+</code> after a chord counts from the chord's end.</p>
    </section>`;
  }

  #rests() {
    return html`<section id="rests">
      <h2>Rests</h2>
      <p>
        <code>_</code> followed by a length is a rest: silence that starts where the previous note ends. It plays
        nothing; it just moves the point the next <code>+offset</code> counts from.
      </p>
      ${this.#code(
        `
@piano = "${PIANO}";

melody @phrase (sample=@piano, rootNote="C3") {
    C5 0 1/4, C5 +0 1/4, C5 +0 1/4,
    _ 1/4,                       // one beat of silence
    C5 +0 1/4, C5 +0 1/4, C5 +0 1/2,
};

song { play @phrase (repeat=2); }`,
        'Rests',
      )}
      <p>Rests add up, and a rest may come first. A rest only takes a length: <code>_ 1/4</code>, never a start.</p>
    </section>`;
  }

  #loop() {
    return html`<section id="loop">
      <h2>loop</h2>
      <p>A <code>loop</code> is a section: it stacks things together and has a length in bars.</p>
      ${this.#code(`
loop @groove (bars=1) {
    play @kickPart;
    play @snarePart;
    play @hatPart;
}`)}
      <table>
        <tr>
          <th>Parameter</th>
          <th>Default</th>
          <th>Meaning</th>
        </tr>
        <tr>
          <td><code>bars</code></td>
          <td>inferred</td>
          <td>
            The loop's length in bars (any number greater than 0). Without it, the loop lasts as long as its longest
            content — or one bar if nothing inside takes time.
          </td>
        </tr>
      </table>
      <h3>Together or one after the other</h3>
      <p>This is the one timing rule worth learning by heart:</p>
      <ul>
        <li>
          <strong>Beats and melodies</strong> inside a loop all start <strong>together</strong>, at the top of the
          loop. That's how layers are built.
        </li>
        <li>
          <strong>Loops</strong> inside a loop (a nested <code>loop { }</code>, or <code>play</code> of a loop) play
          <strong>one after the other</strong>, each starting where the previous statement ended.
        </li>
      </ul>
      ${this.#code(
        `
@kick = "${KICK}";
@snare = "${SNARE}";
@hat = "${HAT}";

beat @kickPart  (sample=@kick)  { 1, 9 }
beat @snarePart (sample=@snare) { 5, 13 }
beat @hatPart   (sample=@hat)   { x.x.|x.x.|x.x.|x.x. }

// layers: everything starts together
loop @verse (bars=2) {
    play @kickPart (repeat=2);
    play @snarePart (repeat=2);
}

loop @chorus (bars=2) {
    play @kickPart (repeat=2);
    play @snarePart (repeat=2);
    play @hatPart (repeat=2);
}

// sections: loops play in sequence
loop @part {
    play @verse;
    play @chorus;
}

song { play @part (repeat=2); }`,
        'Layers and sections',
      )}
      <div class="note">
        Because a played loop waits for whatever came before it, mixing the two in one loop can surprise you:
        <code>play @chords; play @drums;</code> — where <code>@drums</code> is a loop — starts the drums
        <em>after</em> the chords. To layer drums under chords, play the drum <code>beat</code>s directly in that
        loop, or put the loop first.
      </div>
      <p>
        A loop body may also declare a <code>beat</code>, <code>melody</code> or <code>loop</code> inline; those play
        right there but can't be referenced from elsewhere. A loop that ends up playing itself is an error.
      </p>
    </section>`;
  }

  #song() {
    return html`<section id="song">
      <h2>song</h2>
      <p>The <code>song</code> is the track's timeline. Read top to bottom, it is the arrangement:</p>
      ${this.#code(`
song {
    play @intro;
    play @verse (repeat=2);
    play @chorus;
    play @outro;
}`)}
      <p>
        Every file needs exactly one <code>song</code>. Its body may only contain <code>play</code> statements —
        declare beats, melodies and loops at the top level and play them here.
      </p>
      <h3>play</h3>
      <table>
        <tr>
          <th>Parameter</th>
          <th>Default</th>
          <th>Meaning</th>
        </tr>
        <tr>
          <td><code>repeat</code></td>
          <td><code>1</code></td>
          <td>
            Play the target this many times in a row (a whole number; <code>0</code> mutes it). A beat repeats every
            bar, a melody every whole number of bars it spans, a loop every time its body ends.
          </td>
        </tr>
      </table>
      <p><code>play</code> can target any top-level <code>beat</code>, <code>melody</code> or <code>loop</code>.</p>
    </section>`;
  }

  #timing() {
    return html`<section id="timing">
      <h2>Timing</h2>
      <p>Two different units, easy to mix up:</p>
      <ul>
        <li>
          <strong><code>grid</code></strong> is a beat's <em>resolution</em> — how finely one bar is sliced. Changing
          it changes how precise the steps are, not how long the beat lasts.
        </li>
        <li>
          <strong><code>bars</code></strong> is a loop's <em>length</em>. A bar is <code>@beatsPerBar</code> beats —
          the "1, 2, 3, 4" you count.
        </li>
      </ul>
      <table>
        <tr>
          <th>Unit</th>
          <th>Length</th>
          <th>At 120 BPM, 4/4, grid=16</th>
        </tr>
        <tr>
          <td>beat</td>
          <td><code>60 / bpm</code> seconds</td>
          <td>0.5 s</td>
        </tr>
        <tr>
          <td>bar</td>
          <td>beat × <code>@beatsPerBar</code></td>
          <td>2 s</td>
        </tr>
        <tr>
          <td>step</td>
          <td>bar / <code>grid</code></td>
          <td>0.125 s</td>
        </tr>
        <tr>
          <td>melody <code>1/4</code></td>
          <td>a quarter of a bar</td>
          <td>0.5 s</td>
        </tr>
      </table>
      <p>Use <code>grid=32</code> for hi-hat rolls, <code>grid=12</code> for triplet feels.</p>
    </section>`;
  }

  #sounds() {
    return html`<section id="sounds">
      <h2>Sounds</h2>
      <p>
        These sounds come with the playground. Copy a line into your song, then use the variable as a
        <code>sample</code>. Any other URL you can reach works too (the server has to allow cross-origin requests).
      </p>
      <div class="sounds">
        ${sampleGroups.map(
          (group) => html`<div>
            <h3>${group.name}</h3>
            ${group.samples.map((sample) => {
              const line = `@${toVariableName(sample.label)} = "${sample.url}";`;
              return html`<div class="sound">
                <span class="label">${sample.label}</span>
                <code title=${sample.url}>${sample.rootNote ? `rootNote="${sample.rootNote}"` : sample.url}</code>
                <wa-copy-button value=${line} copy-label="Copy variable" success-label="Copied!"></wa-copy-button>
              </div>`;
            })}
          </div>`,
        )}
      </div>
      <p>
        For the tonal sounds, the code column shows the <code>rootNote</code> to use in the melody, e.g.
        <code>melody @lead (sample=@piano, rootNote="C3") { ... }</code>.
      </p>
    </section>`;
  }

  #diagnostics() {
    return html`<section id="diagnostics">
      <h2>Errors & warnings</h2>
      <p>
        The playground checks your song as you type. <strong>Errors</strong> (red) mean it can't play;
        <strong>warnings</strong> (yellow) mean it plays, but something in it has no effect. Each one points at a line
        and column.
      </p>
      <table>
        <tr>
          <th>You'll see it when…</th>
          <th>Kind</th>
        </tr>
        <tr>
          <td>You <code>play</code> something that doesn't exist (it suggests the closest name), or a variable</td>
          <td>error</td>
        </tr>
        <tr>
          <td>Two beats, melodies or loops share a name</td>
          <td>error</td>
        </tr>
        <tr>
          <td>A parameter has the wrong type (<code>grid="abc"</code>, <code>bars=0</code>, <code>repeat=-1</code>)</td>
          <td>error</td>
        </tr>
        <tr>
          <td>A beat or melody has no <code>sample</code></td>
          <td>error</td>
        </tr>
        <tr>
          <td>A step is outside the beat's grid</td>
          <td>error</td>
        </tr>
        <tr>
          <td>A loop plays itself, directly or through other loops</td>
          <td>error</td>
        </tr>
        <tr>
          <td>There's no <code>song</code>, or more than one, or a <code>play</code> outside a loop/song</td>
          <td>error</td>
        </tr>
        <tr>
          <td>A beat, melody or loop is never played, or a variable never used</td>
          <td>warning</td>
        </tr>
        <tr>
          <td>A parameter isn't used by anything (it suggests a fix for typos like <code>grdi</code>)</td>
          <td>warning</td>
        </tr>
        <tr>
          <td>A note lasts 0, or a step is listed twice</td>
          <td>warning</td>
        </tr>
        <tr>
          <td>A variable is assigned inside a loop or song (only top-level assignments count)</td>
          <td>warning</td>
        </tr>
      </table>
    </section>`;
  }

  #playground() {
    return html`<section id="playground">
      <h2>Using the playground</h2>
      <ul>
        <li>
          <strong>Tabs</strong> keep several songs open. Everything is saved in this browser as you type, so you can
          close the page and come back later. Double-click a tab to rename it.
        </li>
        <li><strong>Examples</strong> opens a ready-made song in a new tab — a good place to start tweaking.</li>
        <li>
          The <strong>player bar</strong> shows the song's length before you press play, with one lane per sound.
          Click (or drag) anywhere on it to play from that point; <kbd>←</kbd>/<kbd>→</kbd> jump 2 seconds when it's
          focused.
        </li>
        <li>
          <strong>Parts</strong> has a card for every top-level <code>beat</code>, <code>melody</code> and
          <code>loop</code>. Its ▶ plays just that part — handy for checking one layer, and it works even while
          the <code>song</code> block has errors.
        </li>
        <li><strong>Try it</strong> buttons in these docs do the same with each snippet.</li>
        <li>
          <strong>Ctrl + Enter</strong> (<strong>⌘ + Enter</strong> on a Mac) plays or stops the current song.
        </li>
        <li>
          Songs live only in this browser. To keep one somewhere else, copy its code into a <code>.spin</code> file —
          the same code runs in the SpinScript CLI and the VS Code extension.
        </li>
      </ul>
    </section>`;
  }

  #cheatsheet() {
    return html`<section id="cheatsheet">
      <h2>Cheat sheet</h2>
      ${this.#code(`
@bpm = 120;                          // settings: @bpm, @beatsPerBar
@kick = "/path/to/kick.wav";         // a variable

beat @k (sample=@kick, grid=16) { 1, 5, 9, 13 }   // steps from 1
beat @h (sample=@hat) { x.x.|x.x.|x.x.|x.xx }     // or a drum row

melody @m (sample=@piano, rootNote="C3") {
    C4 0 1/4,          // NOTE START LENGTH (in bars)
    E4 +0 1/4,         // +0: right after the previous note
    _ 1/4,             // rest
    [C4 E4 G4] +0 1/2, // chord, spelled out
    A3:m +0 1/2,       // chord, by name
};

loop @section (bars=4) {   // beats/melodies: together
    play @k (repeat=4);
    play @m;
}

song {                     // loops: one after the other
    play @section (repeat=2);
}`)}
    </section>`;
  }
}

// "Snare BoomBap" -> "snareBoomBap", "808 clean" -> "s808Clean"
function toVariableName(label: string): string {
  const words = label.split(/[^A-Za-z0-9]+/).filter(Boolean);
  const name = words.map((w, i) => (i === 0 ? w.toLowerCase() : w[0].toUpperCase() + w.slice(1).toLowerCase())).join('');
  return /^[A-Za-z]/.test(name) ? name : `s${name}`;
}

customElements.define('spinscript-docs', SpinScriptDocs);
