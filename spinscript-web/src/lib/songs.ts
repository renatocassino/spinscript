// Songs live in localStorage so a visitor can close the tab and pick up
// where they left off. Every read/write is guarded: storage can be missing
// or throw (private windows, blocked site data), and the playground must
// still work — it just won't remember anything in that case.

export interface Song {
  id: string;
  name: string;
  code: string;
  updatedAt: number;
}

export interface SongLibrary {
  activeId: string;
  songs: Song[];
}

const STORAGE_KEY = 'spinscript.songs.v1';
// Before tabs existed, the playground kept a single song under this key.
const LEGACY_KEY = 'spincode';

export const NEW_SONG_CODE = `@bpm = 120;

@kick = "/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Punchy.wav";

beat @kickPart (sample=@kick) { 1, 5, 9, 13 }

song {
    play @kickPart (repeat=4);
}
`;

export function newId(): string {
  return typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

export function createSong(name: string, code: string): Song {
  return { id: newId(), name, code, updatedAt: Date.now() };
}

function read(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function isSong(value: unknown): value is Song {
  const song = value as Song;
  return typeof song === 'object' && song !== null && typeof song.id === 'string' && typeof song.name === 'string' && typeof song.code === 'string';
}

function parse(raw: string | null): SongLibrary | null {
  if (!raw) return null;
  try {
    const data = JSON.parse(raw) as Partial<SongLibrary>;
    const songs = Array.isArray(data.songs) ? data.songs.filter(isSong) : [];
    if (songs.length === 0) return null;
    const activeId = songs.some((s) => s.id === data.activeId) ? data.activeId! : songs[0].id;
    return { activeId, songs };
  } catch {
    return null;
  }
}

/** The saved library, or a fresh one (migrating the pre-tabs single song). */
export function loadLibrary(): SongLibrary {
  const saved = parse(read(STORAGE_KEY));
  if (saved) return saved;

  const legacy = read(LEGACY_KEY);
  const first = createSong(legacy ? 'My song' : 'Untitled', legacy ?? NEW_SONG_CODE);
  return { activeId: first.id, songs: [first] };
}

export function saveLibrary(library: SongLibrary): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(library));
  } catch {
    // Storage unavailable or full: keep working in memory.
  }
}

/**
 * Calls `onChange` when another browser tab saves the library, so two open
 * playground tabs don't silently overwrite each other's songs.
 */
export function watchLibrary(onChange: (library: SongLibrary) => void): () => void {
  const listener = (event: StorageEvent) => {
    if (event.key !== STORAGE_KEY) return;
    const library = parse(event.newValue);
    if (library) onChange(library);
  };
  window.addEventListener('storage', listener);
  return () => window.removeEventListener('storage', listener);
}

/** "Untitled", "Untitled 2", ... — the first name not already taken. */
export function uniqueName(base: string, songs: Song[]): string {
  const taken = new Set(songs.map((s) => s.name));
  if (!taken.has(base)) return base;
  let n = 2;
  while (taken.has(`${base} ${n}`)) n++;
  return `${base} ${n}`;
}
