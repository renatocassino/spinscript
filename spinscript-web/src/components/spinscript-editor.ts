import { LitElement, css, html } from 'lit';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

import '@awesome.me/webawesome/dist/components/card/card.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/callout/callout.js';
import '@awesome.me/webawesome/dist/components/tag/tag.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/drawer/drawer.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

import './spinscript-docs';
import { examples } from '../lib/examples';
import { highlightSpinScript, lineNumbers, tokenStyles } from '../lib/highlight';
import {
  NEW_SONG_CODE,
  createSong,
  loadLibrary,
  saveLibrary,
  uniqueName,
  watchLibrary,
  type Song,
  type SongLibrary,
} from '../lib/songs';

// Mirrors the Compile() output of SpinScript.Core/Json/SpinScriptJson.cs:
// `type` tells the two event shapes apart, and `rate` only exists on melody
// events.
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
  duration: number;
}

// Parse() and Compile() never throw across the JS boundary: failures come
// back as this shape.
interface CompileError {
  error: string;
}

// Mirrors Check() in SpinScript.Core/Json/SpinScriptJson.cs: 0-indexed positions.
interface Diagnostic {
  severity: 'error' | 'warning';
  code: string;
  message: string;
  line: number;
  column: number;
}

function isCompileError(value: unknown): value is CompileError {
  return typeof value === 'object' && value !== null && 'error' in value;
}

interface SpinScriptExports {
  Parse(source: string): string;
  Check(source: string): string;
  Compile(source: string): string;
}

const THEME_STORAGE_KEY = 'spintheme';

// How long typing has to pause before the song is saved and re-checked.
const IDLE_MS = 300;

export class SpinScriptEditor extends LitElement {
  static properties = {
    library: { state: true },
    error: { state: true },
    diagnostics: { state: true },
    isPlaying: { state: true },
    ready: { state: true },
    theme: { state: true },
    docsOpen: { state: true },
    renamingId: { state: true },
  };

  declare library: SongLibrary;
  declare error: string;
  declare diagnostics: Diagnostic[];
  declare isPlaying: boolean;
  declare ready: boolean;
  declare theme: 'light' | 'dark';
  declare docsOpen: boolean;
  declare renamingId: string | null;

  #spinscript: SpinScriptExports | null = null;
  #audioContext: AudioContext | null = null;
  #sampleBufferCache = new Map<string, AudioBuffer>();
  #pendingEvents: SpinScriptEvent[] = [];
  #activeSources = new Set<AudioBufferSourceNode>();
  #schedulerTimer: ReturnType<typeof setTimeout> | null = null;
  #idleTimer: ReturnType<typeof setTimeout> | null = null;
  #unwatchLibrary: (() => void) | null = null;

  static styles = [
    tokenStyles,
    css`
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
        gap: var(--wa-space-s);
      }

      .card-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
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

      .header-actions {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: var(--wa-space-xs);
      }

      .status-tag {
        flex-shrink: 0;
        gap: var(--wa-space-2xs);
      }

      .status-tag wa-spinner {
        font-size: 0.85em;
      }

      .example-item {
        display: flex;
        flex-direction: column;
        max-width: 22rem;
        white-space: normal;
        line-height: 1.35;
      }

      .example-item small {
        color: var(--wa-color-text-quiet);
      }

      /* Song tabs */
      .tabs {
        display: flex;
        align-items: flex-end;
        gap: var(--wa-space-3xs);
        overflow-x: auto;
        border-bottom: 1px solid var(--wa-color-surface-border);
        scrollbar-width: thin;
      }

      .tab {
        display: flex;
        align-items: center;
        gap: var(--wa-space-3xs);
        flex-shrink: 0;
        max-width: 14rem;
        padding: var(--wa-space-2xs) var(--wa-space-2xs) var(--wa-space-2xs) var(--wa-space-s);
        border: 1px solid transparent;
        border-bottom: none;
        border-radius: var(--wa-border-radius-m) var(--wa-border-radius-m) 0 0;
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-s);
        cursor: pointer;
        margin-bottom: -1px;
        background: none;
        font-family: inherit;
      }

      .tab:hover {
        color: var(--wa-color-text-normal);
        background: var(--wa-color-surface-lowered);
      }

      .tab[aria-selected='true'] {
        color: var(--wa-color-text-normal);
        background: var(--wa-color-surface-default);
        border-color: var(--wa-color-surface-border);
        font-weight: var(--wa-font-weight-semibold);
      }

      .tab .name {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }

      .tab input {
        font: inherit;
        width: 9rem;
        padding: 0 var(--wa-space-3xs);
        border: 1px solid var(--wa-color-brand-border-normal);
        border-radius: var(--wa-border-radius-s);
        background: var(--wa-color-surface-default);
        color: var(--wa-color-text-normal);
      }

      .tab .close {
        display: inline-grid;
        place-items: center;
        width: 1.4em;
        height: 1.4em;
        border: none;
        border-radius: 50%;
        background: none;
        color: inherit;
        cursor: pointer;
        font-size: 1em;
        line-height: 1;
        opacity: 0.6;
      }

      .tab .close:hover {
        opacity: 1;
        background: var(--wa-color-surface-border);
      }

      .new-tab {
        flex-shrink: 0;
        margin-bottom: var(--wa-space-3xs);
      }

      /* Code editor: a transparent textarea over a highlighted <pre> */
      .editor {
        --gutter-width: 3em;
        position: relative;
        width: 100%;
        height: min(60vh, 560px);
        min-height: 320px;
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
        outline: none;
      }

      .toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--wa-space-m);
      }

      .hint {
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-xs);
      }

      kbd {
        font-family: var(--wa-font-family-code);
        font-size: 0.9em;
        padding: 0 0.3em;
        border: 1px solid var(--wa-color-surface-border);
        border-radius: var(--wa-border-radius-s);
      }

      .diagnostics ul {
        margin: 0;
        padding-left: 1.25rem;
      }

      .diagnostic-position {
        font-family: var(--wa-font-family-code, monospace);
        opacity: 0.7;
      }

      .docs-drawer {
        --size: 85vh;
      }

      .docs-drawer::part(body) {
        padding-top: 0;
      }
    `,
  ];

  constructor() {
    super();
    this.library = loadLibrary();
    this.error = '';
    this.diagnostics = [];
    this.isPlaying = false;
    this.ready = false;
    this.docsOpen = false;
    this.renamingId = null;
    // The inline script in index.astro already applied wa-light/wa-dark to
    // <html> before this component upgraded, so just read it back.
    this.theme = document.documentElement.classList.contains('wa-dark') ? 'dark' : 'light';
  }

  get #activeSong(): Song {
    return this.library.songs.find((s) => s.id === this.library.activeId) ?? this.library.songs[0];
  }

  override connectedCallback(): void {
    super.connectedCallback();
    this.#unwatchLibrary = watchLibrary((incoming) => this.#mergeExternalLibrary(incoming));
    window.addEventListener('keydown', this.#handleKeydown);
    window.addEventListener('pagehide', this.#flush);
    void this.#init();
  }

  override disconnectedCallback(): void {
    super.disconnectedCallback();
    this.#unwatchLibrary?.();
    window.removeEventListener('keydown', this.#handleKeydown);
    window.removeEventListener('pagehide', this.#flush);
    this.#stop();
  }

  async #init(): Promise<void> {
    try {
      // public/wasm/_framework is populated by `npm run sync-wasm` from the
      // SpinScript.Wasm build output, not part of the TS/Vite module graph.
      // The URL is built from a variable (not a literal) so Vite's
      // dependency scanner doesn't try to resolve it at build time.
      const dotnetUrl = `${location.origin}/wasm/_framework/dotnet.js`;
      const { dotnet } = await import(/* @vite-ignore */ dotnetUrl);
      const { getAssemblyExports } = await dotnet.create();
      const exports = await getAssemblyExports('SpinScript.Wasm.dll');
      this.#spinscript = exports.SpinScript.Wasm.Exports as SpinScriptExports;
      this.ready = true;
      this.#check();
    } catch (e) {
      console.error(e);
      this.error = `Couldn't load the SpinScript engine: ${(e as Error).message}`;
    }
  }

  // ---------------------------------------------------------------------
  // Songs (tabs)
  // ---------------------------------------------------------------------

  #commit(library: SongLibrary): void {
    this.library = library;
    saveLibrary(library);
  }

  #updateActive(changes: Partial<Song>): void {
    const active = this.#activeSong;
    this.library = {
      ...this.library,
      songs: this.library.songs.map((s) => (s.id === active.id ? { ...s, ...changes, updatedAt: Date.now() } : s)),
    };
  }

  #select(id: string): void {
    if (id === this.library.activeId) return;
    this.#flush();
    this.#stop();
    this.#commit({ ...this.library, activeId: id });
    this.error = '';
    this.#check();
  }

  #openSong(name: string, code: string): void {
    this.#flush();
    this.#stop();
    const song = createSong(uniqueName(name, this.library.songs), code);
    this.#commit({ activeId: song.id, songs: [...this.library.songs, song] });
    this.error = '';
    this.#check();
  }

  #newSong(): void {
    this.#openSong('Untitled', NEW_SONG_CODE);
  }

  #close(id: string, event: Event): void {
    event.stopPropagation();
    const song = this.library.songs.find((s) => s.id === id);
    if (!song) return;
    if (song.code.trim() && !confirm(`Close "${song.name}"? Its code will be deleted from this browser.`)) return;

    const index = this.library.songs.indexOf(song);
    let songs = this.library.songs.filter((s) => s.id !== id);
    if (songs.length === 0) songs = [createSong('Untitled', NEW_SONG_CODE)];
    const activeId = id === this.library.activeId ? songs[Math.min(index, songs.length - 1)].id : this.library.activeId;
    if (id === this.library.activeId) this.#stop();
    this.#commit({ activeId, songs });
    this.#check();
  }

  #startRename(id: string): void {
    this.renamingId = id;
    void this.updateComplete.then(() => {
      const input = this.shadowRoot!.querySelector<HTMLInputElement>('.tab input');
      input?.focus();
      input?.select();
    });
  }

  #finishRename(event: Event, save: boolean): void {
    const id = this.renamingId;
    if (!id) return;
    this.renamingId = null;
    const name = (event.target as HTMLInputElement).value.trim();
    if (!save || !name) return;
    this.#commit({
      ...this.library,
      songs: this.library.songs.map((s) => (s.id === id ? { ...s, name, updatedAt: Date.now() } : s)),
    });
  }

  #handleRenameKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') this.#finishRename(event, true);
    if (event.key === 'Escape') this.#finishRename(event, false);
  }

  // Another browser tab saved: take its songs, but keep this tab's own
  // selection and don't clobber the song being edited here mid-typing.
  #mergeExternalLibrary(incoming: SongLibrary): void {
    const editing = this.#idleTimer !== null ? this.#activeSong : null;
    const songs = incoming.songs.map((s) => (editing && s.id === editing.id ? editing : s));
    const activeId = songs.some((s) => s.id === this.library.activeId) ? this.library.activeId : songs[0].id;
    const codeChanged = this.#activeSong.code !== songs.find((s) => s.id === activeId)?.code;
    this.library = { activeId, songs };
    if (codeChanged) this.#check();
  }

  // ---------------------------------------------------------------------
  // Editing
  // ---------------------------------------------------------------------

  #handleInput(event: Event): void {
    this.#updateActive({ code: (event.target as HTMLTextAreaElement).value });
    if (this.#idleTimer) clearTimeout(this.#idleTimer);
    this.#idleTimer = setTimeout(this.#flush, IDLE_MS);
  }

  // Saves pending edits and re-checks them; bound so it can be a listener.
  #flush = (): void => {
    if (this.#idleTimer === null) return;
    clearTimeout(this.#idleTimer);
    this.#idleTimer = null;
    saveLibrary(this.library);
    this.#check();
  };

  #check(): void {
    if (!this.#spinscript) return;
    try {
      this.diagnostics = JSON.parse(this.#spinscript.Check(this.#activeSong.code)) as Diagnostic[];
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
    }
  }

  #handleScroll(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    const pre = this.shadowRoot!.getElementById('highlight')!;
    pre.scrollTop = textarea.scrollTop;
    pre.scrollLeft = textarea.scrollLeft;
    this.shadowRoot!.getElementById('gutter')!.scrollTop = textarea.scrollTop;
  }

  // Tab inserts spaces instead of leaving the editor; Ctrl/Cmd+Enter plays.
  #handleEditorKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || event.ctrlKey || event.metaKey || event.altKey) return;
    event.preventDefault();
    const textarea = event.target as HTMLTextAreaElement;
    textarea.setRangeText('    ', textarea.selectionStart, textarea.selectionEnd, 'end');
    textarea.dispatchEvent(new Event('input'));
  }

  #handleKeydown = (event: KeyboardEvent): void => {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      void this.#togglePlay();
    }
  };

  #toggleTheme(): void {
    const next = this.theme === 'dark' ? 'light' : 'dark';
    this.theme = next;
    document.documentElement.classList.remove('wa-light', 'wa-dark');
    document.documentElement.classList.add(next === 'dark' ? 'wa-dark' : 'wa-light');
    try {
      localStorage.setItem(THEME_STORAGE_KEY, next);
    } catch {
      // Not remembered; the toggle still works for this visit.
    }
  }

  #handleExampleSelect(event: CustomEvent<{ item: HTMLElement & { value: string } }>): void {
    const example = examples.find((e) => e.id === event.detail.item.value);
    if (example) this.#openSong(example.title, example.code);
  }

  #handleTryCode(event: CustomEvent<{ title: string; code: string }>): void {
    this.docsOpen = false;
    this.#openSong(event.detail.title, event.detail.code);
  }

  // ---------------------------------------------------------------------
  // Playback
  // ---------------------------------------------------------------------

  #ensureAudioContext(): AudioContext {
    this.#audioContext ??= new AudioContext();
    return this.#audioContext;
  }

  async #loadSample(url: string): Promise<AudioBuffer> {
    const cached = this.#sampleBufferCache.get(url);
    if (cached) return cached;

    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Couldn't load sample "${url}" (HTTP ${response.status}).`);
    }
    const audioBuffer = await this.#ensureAudioContext().decodeAudioData(await response.arrayBuffer());
    this.#sampleBufferCache.set(url, audioBuffer);
    return audioBuffer;
  }

  async #loadSamples(urls: string[]): Promise<Map<string, AudioBuffer>> {
    const buffers = await Promise.all(urls.map((url) => this.#loadSample(url)));
    return new Map(urls.map((url, i) => [url, buffers[i]]));
  }

  // Schedules `buffer` on the AudioContext clock (sample-accurate, unlike
  // setTimeout). `rate` is a melody note's playback rate; omit it for drums.
  #playSample(buffer: AudioBuffer, delayMs = 0, rate?: number): void {
    const audioContext = this.#ensureAudioContext();
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    if (rate !== undefined) {
      source.playbackRate.value = rate;
    }
    source.connect(audioContext.destination);
    source.onended = () => this.#activeSources.delete(source);
    this.#activeSources.add(source);
    source.start(audioContext.currentTime + delayMs / 1000);
  }

  async #togglePlay(): Promise<void> {
    if (this.isPlaying) {
      this.#stop();
    } else {
      await this.#play();
    }
  }

  #stop(): void {
    if (this.#schedulerTimer) clearTimeout(this.#schedulerTimer);
    this.#schedulerTimer = null;
    this.#pendingEvents = [];
    for (const source of this.#activeSources) {
      try {
        source.stop();
      } catch {
        // Already stopped.
      }
    }
    this.#activeSources.clear();
    this.isPlaying = false;
  }

  async #play(): Promise<void> {
    this.#flush();
    this.error = '';
    if (!this.#spinscript) return;

    try {
      const parsed: unknown = JSON.parse(this.#spinscript.Compile(this.#activeSong.code));
      if (isCompileError(parsed)) {
        throw new Error(parsed.error);
      }

      const result = parsed as CompileResult;
      const buffers = await this.#loadSamples([...new Set(result.events.map((e) => e.sample))]);

      const audioContext = this.#ensureAudioContext();
      await audioContext.resume();

      this.isPlaying = true;
      this.#pendingEvents = [...result.events];
      const startAudioTime = audioContext.currentTime;
      const lookaheadMs = 300;
      const schedulerIntervalMs = 25;

      const scheduleNext = (): void => {
        const now = audioContext.currentTime;

        this.#pendingEvents = this.#pendingEvents.filter((event) => {
          const timeToStart = (startAudioTime + event.time / 1000 - now) * 1000;
          if (timeToStart >= lookaheadMs) return true;

          const buffer = buffers.get(event.sample);
          if (buffer) {
            this.#playSample(buffer, Math.max(timeToStart, 0), event.type === 'melody' ? event.rate : undefined);
          }
          return false;
        });

        if (this.#pendingEvents.length === 0) {
          // Everything is scheduled; flip back to "Play" when the song ends.
          const remainingMs = Math.max(0, startAudioTime * 1000 + result.duration - now * 1000);
          this.#schedulerTimer = setTimeout(() => this.#stop(), remainingMs + 200);
          return;
        }

        this.#schedulerTimer = setTimeout(scheduleNext, schedulerIntervalMs);
      };

      scheduleNext();
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
      this.#stop();
    }
  }

  // ---------------------------------------------------------------------
  // Rendering
  // ---------------------------------------------------------------------

  #renderDiagnostics(severity: Diagnostic['severity'], variant: 'danger' | 'warning') {
    const items = this.diagnostics.filter((d) => d.severity === severity);
    if (items.length === 0) return null;

    return html`<wa-callout class="diagnostics" variant=${variant} appearance="outlined" size="small">
      <ul>
        ${items.map(
          (d) => html`<li><span class="diagnostic-position">${d.line + 1}:${d.column + 1}</span> ${d.message}</li>`,
        )}
      </ul>
    </wa-callout>`;
  }

  #renderTabs() {
    return html`<div class="tabs" role="tablist" aria-label="Songs">
      ${this.library.songs.map((song) => {
        const selected = song.id === this.library.activeId;
        return html`<div
          class="tab"
          role="tab"
          tabindex=${selected ? 0 : -1}
          aria-selected=${selected}
          title=${song.name}
          @click=${() => this.#select(song.id)}
          @dblclick=${() => this.#startRename(song.id)}
          @keydown=${(e: KeyboardEvent) => e.key === 'F2' && this.#startRename(song.id)}
        >
          ${this.renamingId === song.id
            ? html`<input
                aria-label="Song name"
                .value=${song.name}
                @click=${(e: Event) => e.stopPropagation()}
                @keydown=${this.#handleRenameKeydown}
                @blur=${(e: Event) => this.#finishRename(e, true)}
              />`
            : html`<span class="name">${song.name}</span>`}
          <button class="close" aria-label="Close ${song.name}" @click=${(e: Event) => this.#close(song.id, e)}>×</button>
        </div>`;
      })}
      <wa-button class="new-tab" size="small" appearance="plain" @click=${this.#newSong} title="New song">+ New</wa-button>
    </div>`;
  }

  override render() {
    const statusVariant = this.isPlaying ? 'success' : this.ready ? 'neutral' : 'brand';
    const statusLabel = this.isPlaying ? 'Playing' : this.ready ? 'Ready' : 'Loading…';
    const code = this.#activeSong.code;
    const hasErrors = this.diagnostics.some((d) => d.severity === 'error');

    return html`
      <wa-card class="editor-card" appearance="outlined">
        <div slot="header" class="card-header">
          <div>
            <h1>SpinScript</h1>
            <p>Music as code — write a beat, press play.</p>
          </div>
          <div class="header-actions">
            <wa-tag class="status-tag" variant=${statusVariant} appearance="filled" size="small" pill>
              ${!this.ready ? html`<wa-spinner></wa-spinner>` : null} ${statusLabel}
            </wa-tag>
            <wa-dropdown @wa-select=${this.#handleExampleSelect}>
              <wa-button slot="trigger" size="small" appearance="outlined" with-caret>Examples</wa-button>
              ${examples.map(
                (example) => html`<wa-dropdown-item value=${example.id}>
                  <span class="example-item">
                    <span>${example.title}</span>
                    <small>${example.description}</small>
                  </span>
                </wa-dropdown-item>`,
              )}
            </wa-dropdown>
            <wa-button size="small" appearance="outlined" @click=${() => (this.docsOpen = true)}>Docs</wa-button>
            <wa-button
              size="small"
              appearance="outlined"
              pill
              @click=${this.#toggleTheme}
              aria-label=${this.theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
            >
              ${this.theme === 'dark' ? '☀️' : '🌙'}
            </wa-button>
          </div>
        </div>

        ${this.#renderTabs()}

        <div class="editor">
          <div id="gutter" class="gutter" aria-hidden="true">${lineNumbers(code)}</div>
          <pre id="highlight" aria-hidden="true"><code>${unsafeHTML(highlightSpinScript(code))}</code></pre>
          <textarea
            id="spincode"
            spellcheck="false"
            aria-label="SpinScript code"
            .value=${code}
            @input=${this.#handleInput}
            @blur=${this.#flush}
            @keydown=${this.#handleEditorKeydown}
            @scroll=${this.#handleScroll}
          ></textarea>
        </div>

        ${this.error
          ? html`<wa-callout variant="danger" appearance="outlined" size="small">${this.error}</wa-callout>`
          : null}
        ${this.#renderDiagnostics('error', 'danger')} ${this.#renderDiagnostics('warning', 'warning')}

        <div slot="footer" class="toolbar">
          <span class="hint">Saved in this browser · <kbd>Ctrl</kbd>+<kbd>Enter</kbd> to play</span>
          <wa-button
            variant=${this.isPlaying ? 'neutral' : 'brand'}
            appearance="accent"
            ?disabled=${!this.ready || (hasErrors && !this.isPlaying)}
            @click=${this.#togglePlay}
          >
            ${this.isPlaying ? 'Stop' : 'Play'}
          </wa-button>
        </div>
      </wa-card>

      <wa-drawer
        class="docs-drawer"
        label="SpinScript documentation"
        placement="bottom"
        light-dismiss
        ?open=${this.docsOpen}
        @wa-after-hide=${(e: Event) => e.target === e.currentTarget && (this.docsOpen = false)}
      >
        <spinscript-docs @try-code=${this.#handleTryCode}></spinscript-docs>
      </wa-drawer>
    `;
  }
}

customElements.define('spinscript-editor', SpinScriptEditor);
