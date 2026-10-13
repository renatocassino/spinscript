// Plays a compiled SpinScript timeline (a list of events, not audio) on the
// Web Audio clock, from any point in it.
//
// Seeking is a filter over the event list: everything that already happened
// before the start point is dropped, except melody notes that are still
// sounding there — those start right away, partway into their sample, so a
// held chord doesn't vanish when you click into the middle of it.

export interface PlayableEvent {
  type: 'sound' | 'melody';
  time: number; // ms
  sample: string;
  duration?: number; // ms, melody notes only
  rate?: number; // playback rate, melody notes only
}

interface ScheduledEvent {
  event: PlayableEvent;
  at: number; // ms from the start point
  offset: number; // ms already elapsed into the note at the start point
}

// How far ahead events are handed to the audio clock, and how often.
const LOOKAHEAD_MS = 300;
const TICK_MS = 25;
// Fade-out at the end of a note, so cutting a sample short doesn't click.
const RELEASE_S = 0.03;
// Small delay before the first event, so it isn't scheduled in the past.
const START_DELAY_S = 0.05;

export class Player extends EventTarget {
  #context: AudioContext | null = null;
  #buffers = new Map<string, Promise<AudioBuffer>>();
  #sources = new Set<AudioBufferSourceNode>();
  #queue: ScheduledEvent[] = [];
  #timer: ReturnType<typeof setTimeout> | null = null;
  #startedAt = 0; // AudioContext time matching #from
  #from = 0;
  #duration = 0;
  #playing = false;

  get playing(): boolean {
    return this.#playing;
  }

  get duration(): number {
    return this.#duration;
  }

  /** Current position in ms; 0 when stopped. */
  get position(): number {
    if (!this.#playing || !this.#context) return 0;
    const elapsed = (this.#context.currentTime - this.#startedAt) * 1000;
    return Math.min(this.#duration, this.#from + Math.max(0, elapsed));
  }

  /** Loads every sample, then plays `events` starting at `fromMs`. */
  async play(events: PlayableEvent[], duration: number, fromMs = 0): Promise<void> {
    this.stop();

    const context = this.#ensureContext();
    await context.resume();
    const urls = [...new Set(events.map((e) => e.sample))];
    const loaded = await Promise.all(urls.map((url) => this.#load(url)));
    const buffers = new Map(urls.map((url, i) => [url, loaded[i]]));

    const from = Math.max(0, Math.min(fromMs, duration));
    this.#queue = events
      .filter((e) => e.time >= from || (e.type === 'melody' && e.time + (e.duration ?? 0) > from))
      .map((event) => ({ event, at: Math.max(0, event.time - from), offset: Math.max(0, from - event.time) }));

    this.#from = from;
    this.#duration = duration;
    this.#startedAt = context.currentTime + START_DELAY_S;
    this.#playing = true;
    this.dispatchEvent(new Event('statechange'));

    const tick = (): void => {
      const now = (context.currentTime - this.#startedAt) * 1000;
      while (this.#queue.length > 0 && this.#queue[0].at - now < LOOKAHEAD_MS) {
        const next = this.#queue.shift()!;
        const buffer = buffers.get(next.event.sample);
        if (buffer) this.#schedule(context, buffer, next);
      }

      if (this.#queue.length > 0) {
        this.#timer = setTimeout(tick, TICK_MS);
      } else {
        // Everything is on the audio clock; stop once the timeline ends.
        const remaining = this.#duration - this.#from - now;
        this.#timer = setTimeout(() => this.#finish(), Math.max(0, remaining) + 100);
      }
    };
    tick();
  }

  stop(): void {
    if (this.#timer) clearTimeout(this.#timer);
    this.#timer = null;
    this.#queue = [];
    for (const source of this.#sources) {
      try {
        source.stop();
      } catch {
        // Already stopped.
      }
    }
    this.#sources.clear();
    if (this.#playing) {
      this.#playing = false;
      this.dispatchEvent(new Event('statechange'));
    }
  }

  #finish(): void {
    this.stop();
    this.dispatchEvent(new Event('ended'));
  }

  #ensureContext(): AudioContext {
    this.#context ??= new AudioContext();
    return this.#context;
  }

  #load(url: string): Promise<AudioBuffer> {
    let buffer = this.#buffers.get(url);
    if (!buffer) {
      buffer = fetch(url)
        .then((response) => {
          if (!response.ok) throw new Error(`Couldn't load sample "${url}" (HTTP ${response.status}).`);
          return response.arrayBuffer();
        })
        .then((data) => this.#ensureContext().decodeAudioData(data));
      // Don't cache failures: the next play retries.
      buffer.catch(() => this.#buffers.delete(url));
      this.#buffers.set(url, buffer);
    }
    return buffer;
  }

  #schedule(context: AudioContext, buffer: AudioBuffer, { event, at, offset }: ScheduledEvent): void {
    const source = context.createBufferSource();
    source.buffer = buffer;
    const when = this.#startedAt + at / 1000;

    if (event.type === 'melody') {
      const rate = event.rate ?? 1;
      source.playbackRate.value = rate;

      // A note lasts its own duration, not the whole sample.
      const remaining = Math.max(0, (event.duration ?? 0) - offset) / 1000;
      if (remaining <= 0) return;
      const end = when + remaining;
      const release = Math.min(RELEASE_S, remaining / 2);
      const gain = context.createGain();
      gain.gain.setValueAtTime(1, when);
      gain.gain.setValueAtTime(1, end - release);
      gain.gain.linearRampToValueAtTime(0, end);
      source.connect(gain).connect(context.destination);

      // The sample is read `rate` times faster than real time.
      const bufferOffset = (offset / 1000) * rate;
      if (bufferOffset >= buffer.duration) return;
      source.start(when, bufferOffset);
      source.stop(end + 0.01);
    } else {
      source.connect(context.destination);
      source.start(when);
    }

    source.onended = () => this.#sources.delete(source);
    this.#sources.add(source);
  }
}
