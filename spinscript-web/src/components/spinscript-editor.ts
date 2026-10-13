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
import './spinscript-parts';
import './spinscript-sounds';
import './spinscript-timeline';
import type { Part } from './spinscript-parts';
import { examples } from '../lib/examples';
import { Player, type PlayableEvent } from '../lib/player';
import { buildMidi, download, exportName, renderWav } from '../lib/export';
import { LOCAL_PREFIX, SoundStore, isLocalUrl, soundVariableName, type SoundInfo } from '../lib/sound-store';
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

// Mirrors the Compile()/CompilePart() output of
// SpinScript.Core/Json/SpinScriptJson.cs.
interface CompileResult {
  events: PlayableEvent[];
  duration: number;
  bpm: number;
  beatsPerBar: number;
}

// What the player bar is showing: the whole song, or one part of it.
interface Timeline {
  part: string | null;
  events: PlayableEvent[];
  duration: number;
  bpm: number;
  beatsPerBar: number;
}

// The bits of Parse()'s AST the part cards need.
interface AstStatement {
  type: string;
  name?: string;
  line?: number;
  parameters?: Record<string, { type: string; value: unknown }>;
  steps?: number[];
  notes?: unknown[];
  statements?: AstStatement[];
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
  CompilePart(source: string, name: string): string;
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
    timeline: { state: true },
    parts: { state: true },
    loading: { state: true },
    sounds: { state: true },
    soundsBusy: { state: true },
    soundsPersistent: { state: true },
    notice: { state: true },
    dropping: { state: true },
    exporting: { state: true },
  };

  declare library: SongLibrary;
  declare error: string;
  declare diagnostics: Diagnostic[];
  declare isPlaying: boolean;
  declare ready: boolean;
  declare theme: 'light' | 'dark';
  declare docsOpen: boolean;
  declare renamingId: string | null;
  declare timeline: Timeline | null;
  declare parts: Part[];
  declare loading: boolean;
  declare sounds: SoundInfo[];
  declare soundsBusy: boolean;
  declare soundsPersistent: boolean;
  declare notice: string;
  declare dropping: boolean;
  declare exporting: boolean;

  #spinscript: SpinScriptExports | null = null;
  #player = new Player();
  #soundStore = new SoundStore();
  #noticeTimer: ReturnType<typeof setTimeout> | null = null;
  #dropDepth = 0;
  // Which part the player bar follows; null = the whole song.
  #timelinePart: string | null = null;
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

      .editor.dropping {
        border-color: var(--wa-color-brand-border-loud);
      }

      .drop-overlay {
        position: absolute;
        inset: 0;
        z-index: 2;
        display: grid;
        place-items: center;
        background: color-mix(in srgb, var(--wa-color-brand-fill-loud) 12%, transparent);
        color: var(--wa-color-text-normal);
        font-weight: var(--wa-font-weight-semibold);
        pointer-events: none;
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

      .editor-card::part(footer) {
        display: flex;
        flex-direction: column;
        gap: var(--wa-space-xs);
      }

      .player {
        display: flex;
        align-items: center;
        gap: var(--wa-space-m);
      }

      .play-button,
      .export {
        flex-shrink: 0;
      }

      .play-button {
        min-width: 7.5rem;
      }

      spinscript-timeline {
        flex: 1;
        min-width: 0;
      }

      .hint {
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-xs);
      }

      @media (max-width: 600px) {
        .player {
          flex-direction: column;
          align-items: stretch;
        }
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
    this.timeline = null;
    this.parts = [];
    this.loading = false;
    this.sounds = [];
    this.soundsBusy = false;
    this.soundsPersistent = true;
    this.notice = '';
    this.dropping = false;
    this.exporting = false;
    this.#player.addEventListener('statechange', () => (this.isPlaying = this.#player.playing));
    this.#player.resolveUrl = (url) => (isLocalUrl(url) ? this.#soundStore.resolve(url) : Promise.resolve(url));
    this.#soundStore.addEventListener('change', () => {
      this.#player.forget(LOCAL_PREFIX);
      void this.#refreshSounds();
    });
    void this.#refreshSounds();
    void this.#soundStore.persistent.then((persistent) => (this.soundsPersistent = persistent));
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
    this.#player.stop();
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
    this.#player.stop();
    this.#commit({ ...this.library, activeId: id });
    this.error = '';
    this.#check();
  }

  #openSong(name: string, code: string): void {
    this.#flush();
    this.#player.stop();
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
    if (id === this.library.activeId) this.#player.stop();
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

  // Re-lints the active song and refreshes what depends on it: the part
  // cards and the player bar (so the total time shows before pressing play).
  #check(): void {
    if (!this.#spinscript) return;
    const code = this.#activeSong.code;
    try {
      this.diagnostics = JSON.parse(this.#spinscript.Check(code)) as Diagnostic[];
      this.parts = this.#readParts(code);
      if (this.#timelinePart && !this.parts.some((p) => p.name === this.#timelinePart)) this.#timelinePart = null;
      this.timeline = this.#compile(this.#timelinePart) ?? (this.#timelinePart ? this.#compile(null) : null);
      if (this.timeline?.part !== this.#timelinePart) this.#timelinePart = null;
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
    }
  }

  /** The song (part = null) or one part, compiled; null if it has errors. */
  #compile(part: string | null): Timeline | null {
    const code = this.#activeSong.code;
    const json = part === null ? this.#spinscript!.Compile(code) : this.#spinscript!.CompilePart(code, part);
    const parsed: unknown = JSON.parse(json);
    if (isCompileError(parsed)) return null;
    const result = parsed as CompileResult;
    return { part, events: result.events, duration: result.duration, bpm: result.bpm, beatsPerBar: result.beatsPerBar };
  }

  #readParts(code: string): Part[] {
    const ast = JSON.parse(this.#spinscript!.Parse(code)) as { statements?: AstStatement[] };
    if (!ast.statements) return this.parts; // syntax error: keep the last good cards

    const number = (s: AstStatement, key: string) => {
      const value = s.parameters?.[key];
      return value?.type === 'number' ? (value.value as number) : null;
    };

    return ast.statements.flatMap((s): Part[] => {
      if (!s.name) return [];
      const line = s.line ?? 0;
      switch (s.type) {
        case 'beat': {
          const hits = s.steps?.length ?? 0;
          return [{ name: s.name, kind: 'beat', line, detail: `${hits} hit${hits === 1 ? '' : 's'} · grid ${number(s, 'grid') ?? 16}` }];
        }
        case 'melody': {
          const notes = s.notes?.length ?? 0;
          return [{ name: s.name, kind: 'melody', line, detail: `${notes} note${notes === 1 ? '' : 's'}` }];
        }
        case 'loop': {
          const bars = number(s, 'bars');
          const items = s.statements?.length ?? 0;
          return [{ name: s.name, kind: 'loop', line, detail: bars !== null ? `${bars} bar${bars === 1 ? '' : 's'}` : `${items} item${items === 1 ? '' : 's'}` }];
        }
        default:
          return [];
      }
    });
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
  // Your sounds
  // ---------------------------------------------------------------------

  async #refreshSounds(): Promise<void> {
    try {
      this.sounds = await this.#soundStore.list();
    } catch (e) {
      console.error(e);
    }
  }

  #say(message: string): void {
    this.notice = message;
    if (this.#noticeTimer) clearTimeout(this.#noticeTimer);
    this.#noticeTimer = setTimeout(() => (this.notice = ''), 5000);
  }

  // Saves each dropped file, then adds a variable for it to the open song.
  async #addFiles(files: File[]): Promise<void> {
    const audio = files.filter((f) => f.type.startsWith('audio/') || /\.(wav|mp3|ogg|oga|flac|m4a|aac|webm|aif|aiff)$/i.test(f.name));
    const skipped = files.length - audio.length;
    this.error = '';
    this.soundsBusy = true;
    const added: string[] = [];
    const failures: string[] = skipped ? [`${skipped} file(s) skipped: not audio.`] : [];

    for (const file of audio) {
      try {
        const sound = await this.#soundStore.add(file, (data) => this.#player.decode(data));
        added.push(this.#insertSound(sound.name));
      } catch (e) {
        failures.push((e as Error).message);
      }
    }

    this.soundsBusy = false;
    if (failures.length) this.error = failures.join(' ');
    if (added.length) this.#say(`Added ${added.join(', ')} to "${this.#activeSong.name}". Use it as sample=${added[0]}.`);
  }

  /**
   * Adds `@name = "local:<sound>";` next to the song's other variables and
   * returns the variable used. A sound the song already references is left
   * alone; a variable name that's taken gets a number.
   */
  #insertSound(soundName: string): string {
    const url = `${LOCAL_PREFIX}${soundName}`;
    const code = this.#activeSong.code;

    const existing = new RegExp(`^(@[A-Za-z][A-Za-z0-9_]*)\\s*=\\s*["']${escapeRegExp(url)}["']`, 'm').exec(code);
    if (existing) return existing[1];

    const base = soundVariableName(soundName);
    const taken = new Set([...code.matchAll(/^@([A-Za-z][A-Za-z0-9_]*)\s*=/gm)].map((m) => m[1]));
    let variable = base;
    for (let n = 2; taken.has(variable); n++) variable = `${base}${n}`;
    const line = `@${variable} = "${url}";`;

    // After the last top-level assignment that comes before the first
    // declaration; otherwise right after the file's leading comments.
    const lines = code.split('\n');
    const firstDeclaration = lines.findIndex((l) => /^(beat|melody|loop|song)\b/.test(l));
    const searchEnd = firstDeclaration === -1 ? lines.length : firstDeclaration;
    let at = -1;
    for (let i = 0; i < searchEnd; i++) {
      if (/^@[A-Za-z][A-Za-z0-9_]*\s*=/.test(lines[i])) at = i + 1;
    }
    if (at === -1) {
      at = 0;
      while (at < lines.length && /^\s*(\/\/.*)?$/.test(lines[at])) at++;
      lines.splice(at, 0, line, '');
    } else {
      lines.splice(at, 0, line);
    }

    this.#updateActive({ code: lines.join('\n') });
    saveLibrary(this.library);
    this.#check();
    return `@${variable}`;
  }

  #handleInsertSound(event: CustomEvent<{ name: string }>): void {
    const before = this.#activeSong.code;
    const variable = this.#insertSound(event.detail.name);
    this.#say(before === this.#activeSong.code ? `${variable} is already in this song.` : `Added ${variable} to "${this.#activeSong.name}".`);
  }

  async #handlePreviewSound(event: CustomEvent<{ name: string }>): Promise<void> {
    this.error = '';
    try {
      await this.#player.preview(`${LOCAL_PREFIX}${event.detail.name}`);
    } catch (e) {
      this.error = (e as Error).message;
    }
  }

  async #handleRemoveSound(event: CustomEvent<{ name: string }>): Promise<void> {
    const name = event.detail.name;
    const usedIn = this.library.songs.filter((s) => s.code.includes(`${LOCAL_PREFIX}${name}`)).map((s) => `"${s.name}"`);
    const warning = usedIn.length ? ` It's used in ${usedIn.join(', ')}, which won't be able to play it anymore.` : '';
    if (!confirm(`Delete the sound "${name}" from this browser?${warning}`)) return;
    this.#player.stop();
    await this.#soundStore.remove(name);
  }

  // Dropping audio straight onto the code editor works too.
  #handleEditorDragEnter(event: DragEvent): void {
    if (!event.dataTransfer?.types.includes('Files')) return;
    event.preventDefault();
    this.#dropDepth++;
    this.dropping = true;
  }

  #handleEditorDragOver(event: DragEvent): void {
    if (!event.dataTransfer?.types.includes('Files')) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
  }

  #handleEditorDragLeave(): void {
    this.#dropDepth = Math.max(0, this.#dropDepth - 1);
    if (this.#dropDepth === 0) this.dropping = false;
  }

  #handleEditorDrop(event: DragEvent): void {
    this.#dropDepth = 0;
    this.dropping = false;
    const files = [...(event.dataTransfer?.files ?? [])];
    if (files.length === 0) return; // plain text drop: let the textarea have it
    event.preventDefault();
    void this.#addFiles(files);
  }

  // ---------------------------------------------------------------------
  // Playback
  // ---------------------------------------------------------------------

  // Play button / Ctrl+Enter: the whole song from the top, or stop.
  async #togglePlay(): Promise<void> {
    if (this.isPlaying) {
      this.#player.stop();
      return;
    }
    this.#flush();
    if (this.#timelinePart !== null) {
      this.#timelinePart = null;
      this.#check();
    }
    await this.#playFrom(0);
  }

  async #playPart(event: CustomEvent<{ name: string }>): Promise<void> {
    this.#flush();
    this.#timelinePart = event.detail.name;
    this.#check();
    if (this.timeline?.part !== event.detail.name) {
      // The part doesn't compile on its own: show why.
      const parsed: unknown = JSON.parse(this.#spinscript!.CompilePart(this.#activeSong.code, event.detail.name));
      this.error = isCompileError(parsed) ? parsed.error : '';
      return;
    }
    await this.#playFrom(0);
  }

  // Clicking the player bar: stop and start again from that point.
  async #seek(event: CustomEvent<{ ms: number }>): Promise<void> {
    this.#flush();
    await this.#playFrom(event.detail.ms);
  }

  async #playFrom(ms: number): Promise<void> {
    this.error = '';
    const timeline = this.timeline;
    if (!timeline || timeline.events.length === 0) return;

    this.loading = true;
    try {
      await this.#player.play(timeline.events, timeline.duration, ms);
    } catch (e) {
      console.error(e);
      this.error = (e as Error).message;
      this.#player.stop();
    } finally {
      this.loading = false;
    }
  }

  // ---------------------------------------------------------------------
  // Export
  // ---------------------------------------------------------------------

  // Exports whatever the player bar shows: the song, or the part last played.
  async #export(event: CustomEvent<{ item: HTMLElement & { value: string } }>): Promise<void> {
    this.#flush();
    const timeline = this.timeline;
    if (!timeline || timeline.events.length === 0) return;

    const name = exportName(this.#activeSong.name, timeline.part);
    this.error = '';
    this.exporting = true;
    try {
      if (event.detail.item.value === 'midi') {
        download(buildMidi(timeline), `${name}.mid`);
        this.#say(`Exported ${name}.mid — one track per sound, drums on channel 10.`);
      } else {
        const wav = await renderWav(timeline, (events) => this.#player.loadAll(events));
        download(wav, `${name}.wav`);
        this.#say(`Exported ${name}.wav (${(wav.size / 1024 / 1024).toFixed(1)} MB).`);
      }
    } catch (e) {
      console.error(e);
      this.error = `Export failed: ${(e as Error).message}`;
    } finally {
      this.exporting = false;
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

        <div
          class="editor ${this.dropping ? 'dropping' : ''}"
          @dragenter=${this.#handleEditorDragEnter}
          @dragover=${this.#handleEditorDragOver}
          @dragleave=${this.#handleEditorDragLeave}
          @drop=${this.#handleEditorDrop}
        >
          ${this.dropping ? html`<div class="drop-overlay">Drop to add the sound to this song</div>` : null}
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
        ${this.notice
          ? html`<wa-callout variant="success" appearance="outlined" size="small">${this.notice}</wa-callout>`
          : null}
        ${this.#renderDiagnostics('error', 'danger')} ${this.#renderDiagnostics('warning', 'warning')}

        <spinscript-parts
          .parts=${this.parts}
          .playing=${this.isPlaying ? this.timeline?.part ?? null : null}
          ?disabled=${!this.ready || this.loading}
          @play-part=${this.#playPart}
          @stop-part=${() => this.#player.stop()}
        ></spinscript-parts>

        <spinscript-sounds
          .sounds=${this.sounds}
          ?busy=${this.soundsBusy}
          ?persistent=${this.soundsPersistent}
          @add-files=${(e: CustomEvent<{ files: File[] }>) => this.#addFiles(e.detail.files)}
          @insert-sound=${this.#handleInsertSound}
          @preview-sound=${this.#handlePreviewSound}
          @remove-sound=${this.#handleRemoveSound}
        ></spinscript-sounds>

        <div slot="footer" class="player">
          <wa-button
            class="play-button"
            variant=${this.isPlaying ? 'neutral' : 'brand'}
            appearance="accent"
            ?loading=${this.loading}
            ?disabled=${!this.ready || (hasErrors && !this.isPlaying)}
            @click=${this.#togglePlay}
            aria-label=${this.isPlaying ? 'Stop' : 'Play the song'}
          >
            ${this.isPlaying ? '■ Stop' : '▶ Play'}
          </wa-button>
          <spinscript-timeline
            .player=${this.#player}
            .events=${this.timeline?.events ?? []}
            .duration=${this.timeline?.duration ?? 0}
            label=${this.timeline?.part ? `Playing only @${this.timeline.part}` : hasErrors ? 'Fix the errors above to play' : 'Song — click anywhere to play from there'}
            @seek=${this.#seek}
          ></spinscript-timeline>
          <wa-dropdown class="export" @wa-select=${this.#export}>
            <wa-button
              slot="trigger"
              appearance="outlined"
              with-caret
              ?loading=${this.exporting}
              ?disabled=${!this.timeline || this.timeline.events.length === 0}
              title=${this.timeline?.part ? `Export only @${this.timeline.part}` : 'Export the song'}
              >Export</wa-button
            >
            <wa-dropdown-item value="wav">
              <span class="example-item"><span>WAV audio</span><small>A recording, exactly as Play sounds</small></span>
            </wa-dropdown-item>
            <wa-dropdown-item value="midi">
              <span class="example-item"><span>MIDI</span><small>Notes and drums, for a DAW</small></span>
            </wa-dropdown-item>
          </wa-dropdown>
        </div>
        <div slot="footer" class="hint">Saved in this browser · <kbd>Ctrl</kbd>+<kbd>Enter</kbd> plays the song</div>
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

function escapeRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
