import { LitElement, css, html } from 'lit';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

import '@awesome.me/webawesome/dist/components/card/card.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/callout/callout.js';
import '@awesome.me/webawesome/dist/components/tag/tag.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/divider/divider.js';

// Mirrors SpinScript.Wasm/Json/EventJsonConverter.cs: `type` discriminates
// between the two event shapes it writes ("sound" for SoundEvent, "melody"
// for MelodyEvent), so `rate` is only ever present on melody events.
interface SpinScriptEvent {
  type: 'sound' | 'melody';
  time: number;
  sample: string;
  velocity?: number;
  duration?: number;
  note?: string;
  rate?: number;
}

interface CompileResult {
  events: SpinScriptEvent[];
}

// Both Parse() and Compile() catch exceptions on the .NET side and encode
// them as this shape instead of throwing across the JS interop boundary.
interface CompileError {
  error: string;
}

function isCompileError(value: unknown): value is CompileError {
  return typeof value === 'object' && value !== null && 'error' in value;
}

interface SpinScriptExports {
  Parse(source: string): string;
  Compile(source: string): string;
}

const STORAGE_KEY = 'spincode';

// Mirrors spinscript-vscode/syntaxes/spinscript.tmLanguage.json, in the same
// precedence order (comments/strings/numbers first, punctuation last).
const TOKEN_REGEX = new RegExp(
  [
    String.raw`(?<comment>/\*[\s\S]*?\*/|//.*)`,
    String.raw`(?<string>"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')`,
    // Must come before `number` so a fraction like `1/4` is captured whole
    // instead of just matching its numerator.
    String.raw`(?<fraction>\b[0-9]+/[0-9]+\b)`,
    String.raw`(?<number>\b[0-9]+(?:\.[0-9]+)?\b)`,
    String.raw`(?<keyword>\b(?:song|loop|beat|melody|play)\b)`,
    // A letter A-G, optionally followed by an octave digit (A4), an
    // accidental (C#, Db), or both (Eb8, F#9). Mirrors Lexer.CheckIsNote.
    String.raw`(?<note>\b[A-G](?:[#b][0-9]?|[0-9])?\b)`,
    // A beat grid like x...x..., x|x|x|x, x...|x..x.|.x.xx: a lowercase 'x'
    // followed by any run of 'x', '.' and '|' with no spaces. Mirrors
    // Lexer.CheckIsPatternGrid — a digit or other letter in the run falls
    // back to an identifier/note instead, hence the trailing lookahead.
    String.raw`(?<patternGrid>\bx[x.|]*(?![A-Za-z0-9_]))`,
    String.raw`(?<variable>@[A-Za-z][A-Za-z0-9_]*)`,
    String.raw`(?<parameter>\b[A-Za-z_][A-Za-z0-9_]*\b(?=\s*=))`,
  ].join('|'),
  'g',
);

function escapeHtml(text: string): string {
  return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function highlightSpinScript(code: string): string {
  let out = '';
  let lastIndex = 0;

  for (const match of code.matchAll(TOKEN_REGEX)) {
    out += escapeHtml(code.slice(lastIndex, match.index));

    const tokenType = Object.keys(match.groups!).find((key) => match.groups![key] !== undefined);
    out += `<span class="tok-${tokenType}">${escapeHtml(match[0])}</span>`;

    lastIndex = match.index! + match[0].length;
  }

  out += escapeHtml(code.slice(lastIndex));

  // Keeps the overlay's scroll height matching the textarea's when the
  // code ends with a trailing newline.
  return out + '\n';
}

// One number per logical line, matching the textarea's line count. Doesn't
// account for visually wrapped lines (white-space: pre-wrap), so numbers can
// drift from the wrapped row once a line is long enough to wrap — an
// accepted trade-off for a plain <textarea>-based editor.
function lineNumbers(code: string): string {
  const count = code.split('\n').length;
  return Array.from({ length: count }, (_, i) => i + 1).join('\n');
}

export class SpinScriptEditor extends LitElement {
  static properties = {
    code: { state: true },
    error: { state: true },
    isPlaying: { state: true },
    ready: { state: true },
  };

  declare code: string;
  declare error: string;
  declare isPlaying: boolean;
  declare ready: boolean;

  #spinscript: SpinScriptExports | null = null;
  #audioContext: AudioContext | null = null;
  #sampleBufferCache = new Map<string, AudioBuffer>();
  #pendingEvents: SpinScriptEvent[] = [];

  static styles = css`
    :host {
      display: block;
      font-family: var(--wa-font-family-body);
    }

    .editor-card {
      width: 100%;
    }

    .editor-card::part(body) {
      display: flex;
      flex-direction: column;
      gap: var(--wa-space-m);
    }

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--wa-space-m);
    }

    .card-header h1 {
      margin: 0;
      font-size: var(--wa-font-size-l);
      font-family: var(--wa-font-family-heading);
    }

    .card-header p {
      margin: var(--wa-space-3xs) 0 0;
      color: var(--wa-color-text-quiet);
      font-size: var(--wa-font-size-s);
    }

    .status-tag {
      flex-shrink: 0;
      gap: var(--wa-space-2xs);
    }

    .status-tag wa-spinner {
      font-size: 0.85em;
    }

    .editor {
      --gutter-width: 3em;
      position: relative;
      width: 100%;
      height: 500px;
      border: 1px solid var(--wa-color-surface-border);
      border-radius: var(--wa-border-radius-m);
      overflow: hidden;
    }

    .gutter {
      margin: 0;
      position: absolute;
      top: 0;
      left: 0;
      bottom: 0;
      width: var(--gutter-width);
      padding: var(--wa-space-s) var(--wa-space-2xs) var(--wa-space-s) 0;
      box-sizing: border-box;
      font-family: var(--wa-font-family-code);
      font-size: 14px;
      line-height: 1.5;
      white-space: pre;
      text-align: right;
      color: var(--wa-color-text-quiet);
      background: var(--wa-color-surface-default);
      border-right: 1px solid var(--wa-color-surface-border);
      overflow: hidden;
      user-select: none;
      pointer-events: none;
    }

    .editor pre,
    .editor textarea {
      margin: 0;
      padding: var(--wa-space-s);
      position: absolute;
      top: 0;
      right: 0;
      bottom: 0;
      left: var(--gutter-width);
      width: calc(100% - var(--gutter-width));
      height: 100%;
      box-sizing: border-box;
      font-family: var(--wa-font-family-code);
      font-size: 14px;
      line-height: 1.5;
      white-space: pre-wrap;
      word-wrap: break-word;
      overflow: auto;
      border: none;
    }

    #highlight {
      color: var(--wa-color-text-normal);
      background: var(--wa-color-surface-default);
      pointer-events: none;
    }

    #spincode {
      resize: none;
      background: transparent;
      color: transparent;
      caret-color: var(--wa-color-text-normal);
    }

    .tok-comment {
      color: #6a737d;
      font-style: italic;
    }
    .tok-string {
      color: #22863a;
    }
    .tok-number,
    .tok-fraction {
      color: #005cc5;
    }
    .tok-keyword {
      color: #d73a49;
      font-weight: bold;
    }
    .tok-note {
      color: #0086b3;
    }
    .tok-patternGrid {
      color: #b08800;
    }
    .tok-variable {
      color: #6f42c1;
    }
    .tok-parameter {
      color: #e36209;
    }

    .toolbar {
      display: flex;
      align-items: center;
      justify-content: flex-end;
    }
  `;

  constructor() {
    super();
    this.code = '';
    this.error = '';
    this.isPlaying = false;
    this.ready = false;
  }

  override connectedCallback(): void {
    super.connectedCallback();
    void this.#init();
  }

  async #init(): Promise<void> {
    // public/wasm/_framework is populated by `npm run sync-wasm` from the
    // SpinScript.Wasm build output, not part of the TS/Vite module graph. The
    // URL is built from a variable (not a literal) so Vite's dependency
    // scanner doesn't try to resolve it as a project file at build time.
    const dotnetUrl = `${location.origin}/wasm/_framework/dotnet.js`;
    const { dotnet } = await import(/* @vite-ignore */ dotnetUrl);
    const { getAssemblyExports } = await dotnet.create();
    const exports = await getAssemblyExports('SpinScript.Wasm.dll');
    this.#spinscript = exports.SpinScript.Wasm.Exports as SpinScriptExports;
    this.ready = true;

    const savedCode = localStorage.getItem(STORAGE_KEY);
    if (savedCode) {
      this.code = savedCode;
      this.#tryParse(savedCode);
    }
  }

  #tryParse(source: string): void {
    if (!this.#spinscript) return;

    try {
      const parsed: unknown = JSON.parse(this.#spinscript.Parse(source));
      this.error = isCompileError(parsed) ? parsed.error : '';
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
    }
  }

  #handleInput(event: Event): void {
    this.code = (event.target as HTMLTextAreaElement).value;
  }

  #handleChange(): void {
    this.error = '';
    localStorage.setItem(STORAGE_KEY, this.code);
    this.#tryParse(this.code);
  }

  #handleScroll(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    const pre = this.shadowRoot!.getElementById('highlight')!;
    pre.scrollTop = textarea.scrollTop;
    pre.scrollLeft = textarea.scrollLeft;

    const gutter = this.shadowRoot!.getElementById('gutter')!;
    gutter.scrollTop = textarea.scrollTop;
  }

  #ensureAudioContext(): AudioContext {
    if (!this.#audioContext) {
      this.#audioContext = new AudioContext();
    }
    return this.#audioContext;
  }

  async #loadSample(url: string): Promise<AudioBuffer> {
    const cached = this.#sampleBufferCache.get(url);
    if (cached) return cached;

    const audioContext = this.#ensureAudioContext();
    const response = await fetch(url);
    const arrayBuffer = await response.arrayBuffer();
    const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);

    this.#sampleBufferCache.set(url, audioBuffer);
    return audioBuffer;
  }

  async #loadSamples(urls: string[]): Promise<Map<string, AudioBuffer>> {
    const buffers = await Promise.all(urls.map((url) => this.#loadSample(url)));
    return new Map(urls.map((url, i) => [url, buffers[i]]));
  }

  // Schedules `buffer` to start playing `delayMs` milliseconds from now (uses
  // the AudioContext clock, not setTimeout, so timing stays sample-accurate).
  // `rate` is a melody note's playback rate (1 = original pitch/speed);
  // omit it for plain sound events.
  #playSample(buffer: AudioBuffer, delayMs = 0, rate?: number): AudioBufferSourceNode {
    const audioContext = this.#ensureAudioContext();
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    if (rate !== undefined) {
      source.playbackRate.value = rate;
    }
    source.connect(audioContext.destination);
    source.start(audioContext.currentTime + delayMs / 1000);
    return source;
  }

  async #handlePlay(): Promise<void> {
    this.error = '';

    if (this.isPlaying || !this.#spinscript) return;

    try {
      const parsed: unknown = JSON.parse(this.#spinscript.Compile(this.code));

      if (isCompileError(parsed)) {
        throw new Error(parsed.error);
      }

      const result = parsed as CompileResult;

      const uniqueSamples = Object.keys(
        result.events.reduce((accum, curr) => ({ ...accum, [curr.sample]: true }), {} as Record<string, boolean>),
      );

      const buffers = await this.#loadSamples(uniqueSamples);

      this.isPlaying = true;
      this.#pendingEvents = [...result.events];

      const audioContext = this.#ensureAudioContext();
      const startAudioTime = audioContext.currentTime;
      const lookaheadMs = 300;
      const schedulerIntervalMs = 25;

      const playNextEvents = (): void => {
        const now = audioContext.currentTime;

        this.#pendingEvents = this.#pendingEvents.filter((event) => {
          const scheduledAt = startAudioTime + event.time / 1000;
          const timeToStart = (scheduledAt - now) * 1000;

          if (timeToStart < lookaheadMs) {
            const buffer = buffers.get(event.sample);
            if (buffer) {
              const rate = event.type === 'melody' ? event.rate : undefined;
              this.#playSample(buffer, Math.max(timeToStart, 0), rate);
            }
            return false;
          }

          return true;
        });

        if (this.#pendingEvents.length === 0 || !this.isPlaying) {
          this.isPlaying = false;
          return;
        }

        setTimeout(() => {
          if (!this.isPlaying) return;
          playNextEvents();
        }, schedulerIntervalMs);
      };

      playNextEvents();
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
      this.isPlaying = false;
    }
  }

  override render() {
    const statusVariant = this.isPlaying ? 'success' : this.ready ? 'neutral' : 'brand';
    const statusLabel = this.isPlaying ? 'Playing' : this.ready ? 'Ready' : 'Loading WASM…';

    return html`
      <wa-card class="editor-card" appearance="outlined">
        <div slot="header" class="card-header">
          <div>
            <h1>SpinScript WASM</h1>
            <p>Type your SpinScript code below:</p>
          </div>
          <wa-tag class="status-tag" variant=${statusVariant} appearance="filled" size="small" pill>
            ${!this.ready ? html`<wa-spinner></wa-spinner>` : null} ${statusLabel}
          </wa-tag>
        </div>

        <div class="editor">
          <div id="gutter" class="gutter" aria-hidden="true">${lineNumbers(this.code)}</div>
          <pre id="highlight" aria-hidden="true"><code>${unsafeHTML(highlightSpinScript(this.code))}</code></pre>
          <textarea
            id="spincode"
            spellcheck="false"
            .value=${this.code}
            @input=${this.#handleInput}
            @change=${this.#handleChange}
            @scroll=${this.#handleScroll}
          ></textarea>
        </div>

        ${this.error
          ? html`<wa-callout variant="danger" appearance="outlined" size="small">${this.error}</wa-callout>`
          : null}

        <div slot="footer" class="toolbar">
          <wa-button
            variant="brand"
            appearance="filled"
            ?disabled=${!this.ready}
            @click=${this.#handlePlay}
          >
            ${this.isPlaying ? 'Stop' : 'Play'}
          </wa-button>
        </div>
      </wa-card>
    `;
  }
}

customElements.define('spinscript-editor', SpinScriptEditor);
