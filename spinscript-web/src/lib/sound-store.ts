// The visitor's own sounds, dropped into the playground. Audio is binary
// and easily several MB, so it lives in IndexedDB rather than localStorage.
// Songs refer to these as "local:<name>" (e.g. "local:my-kick.wav"); the
// player resolves that to the stored file when it plays.
//
// If IndexedDB is unavailable (private windows in some browsers, blocked
// site data), sounds still work for the current visit, kept in memory.

export const LOCAL_PREFIX = 'local:';

export interface StoredSound {
  name: string; // "my-kick.wav" — unique, used in "local:my-kick.wav"
  type: string;
  size: number;
  duration: number; // seconds
  createdAt: number;
  data: Blob;
}

export type SoundInfo = Omit<StoredSound, 'data'>;

const DB_NAME = 'spinscript';
const STORE = 'sounds';
const MAX_BYTES = 25 * 1024 * 1024;

export function isLocalUrl(url: string): boolean {
  return url.startsWith(LOCAL_PREFIX);
}

/** "Kick 808 (final).WAV" -> "kick-808-final.wav" */
export function soundFileName(fileName: string): string {
  const dot = fileName.lastIndexOf('.');
  const base = dot > 0 ? fileName.slice(0, dot) : fileName;
  const extension = dot > 0 ? fileName.slice(dot + 1).toLowerCase().replace(/[^a-z0-9]/g, '') : '';
  const slug =
    base
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'sound';
  return extension ? `${slug}.${extension}` : slug;
}

/** "kick-808-final.wav" -> "kick808Final" (a valid @variable name). */
export function soundVariableName(name: string): string {
  const words = name.replace(/\.[a-z0-9]+$/, '').split('-').filter(Boolean);
  const camel = words.map((w, i) => (i === 0 ? w : w[0].toUpperCase() + w.slice(1))).join('');
  return /^[a-z]/i.test(camel) ? camel : `sound${camel[0]?.toUpperCase() ?? ''}${camel.slice(1)}`;
}

export class SoundStore extends EventTarget {
  #db: Promise<IDBDatabase | null>;
  #memory = new Map<string, StoredSound>();
  #objectUrls = new Map<string, string>();

  constructor() {
    super();
    this.#db = this.#open();
  }

  get persistent(): Promise<boolean> {
    return this.#db.then((db) => db !== null);
  }

  async list(): Promise<SoundInfo[]> {
    const sounds = await this.#all();
    return sounds.map(({ data: _data, ...info }) => info).sort((a, b) => a.createdAt - b.createdAt);
  }

  /**
   * Validates and saves an audio file under a unique name. `decode` must
   * reject for anything the browser can't play, so broken files never end
   * up in a song.
   */
  async add(file: File, decode: (data: ArrayBuffer) => Promise<AudioBuffer>): Promise<SoundInfo> {
    if (file.size > MAX_BYTES) {
      throw new Error(`"${file.name}" is ${(file.size / 1024 / 1024).toFixed(1)} MB; the limit is 25 MB.`);
    }

    let duration: number;
    try {
      duration = (await decode(await file.arrayBuffer())).duration;
    } catch {
      throw new Error(`"${file.name}" isn't an audio file this browser can play. Try WAV, MP3 or OGG.`);
    }

    const taken = new Set((await this.#all()).map((s) => s.name));
    const wanted = soundFileName(file.name);
    let name = wanted;
    for (let n = 2; taken.has(name); n++) {
      name = wanted.replace(/(\.[a-z0-9]+)?$/, `-${n}$1`);
    }

    const sound: StoredSound = {
      name,
      type: file.type || 'audio/*',
      size: file.size,
      duration,
      createdAt: Date.now(),
      data: file,
    };
    await this.#write((store) => store.put(sound), () => this.#memory.set(name, sound));
    this.dispatchEvent(new Event('change'));
    const { data: _data, ...info } = sound;
    return info;
  }

  async remove(name: string): Promise<void> {
    await this.#write((store) => store.delete(name), () => this.#memory.delete(name));
    const url = this.#objectUrls.get(name);
    if (url) URL.revokeObjectURL(url);
    this.#objectUrls.delete(name);
    this.dispatchEvent(new Event('change'));
  }

  /** A fetchable URL for "local:<name>", or null if there's no such sound here. */
  async resolve(localUrl: string): Promise<string | null> {
    const name = localUrl.slice(LOCAL_PREFIX.length);
    const cached = this.#objectUrls.get(name);
    if (cached) return cached;

    const sound = (await this.#all()).find((s) => s.name === name);
    if (!sound) return null;
    const url = URL.createObjectURL(sound.data);
    this.#objectUrls.set(name, url);
    return url;
  }

  // -----------------------------------------------------------------------

  #open(): Promise<IDBDatabase | null> {
    return new Promise((resolve) => {
      try {
        const request = indexedDB.open(DB_NAME, 1);
        request.onupgradeneeded = () => request.result.createObjectStore(STORE, { keyPath: 'name' });
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => resolve(null);
        request.onblocked = () => resolve(null);
      } catch {
        resolve(null);
      }
    });
  }

  async #all(): Promise<StoredSound[]> {
    const db = await this.#db;
    if (!db) return [...this.#memory.values()];
    return new Promise((resolve, reject) => {
      const request = db.transaction(STORE, 'readonly').objectStore(STORE).getAll();
      request.onsuccess = () => resolve(request.result as StoredSound[]);
      request.onerror = () => reject(request.error);
    });
  }

  async #write(change: (store: IDBObjectStore) => IDBRequest, fallback: () => void): Promise<void> {
    const db = await this.#db;
    if (!db) {
      fallback();
      return;
    }
    await new Promise<void>((resolve, reject) => {
      const transaction = db.transaction(STORE, 'readwrite');
      change(transaction.objectStore(STORE));
      transaction.oncomplete = () => resolve();
      transaction.onerror = () => reject(transaction.error);
      transaction.onabort = () => reject(transaction.error ?? new Error('Saving the sound was aborted (storage full?).'));
    });
  }
}
