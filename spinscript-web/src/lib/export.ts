// Exports a compiled timeline as a WAV recording or a Standard MIDI File,
// entirely in the browser.
//
// WAV: the same events Play schedules are rendered into an
// OfflineAudioContext (faster than real time, no speakers), then written
// out as 16-bit PCM. MIDI: one track per sample — melodies keep their pitch
// and length, beats go to the drum channel with a General MIDI note guessed
// from the sample's name.

import { voice, type PlayableEvent } from './player';

export interface ExportableTimeline {
  events: PlayableEvent[];
  duration: number; // ms
  bpm: number;
  beatsPerBar: number;
}

const SAMPLE_RATE = 44100;
const CHANNELS = 2;
// Keep the loudest peak a hair under full scale.
const PEAK = 0.98;

// ---------------------------------------------------------------------------
// WAV
// ---------------------------------------------------------------------------

export async function renderWav(
  timeline: ExportableTimeline,
  loadAll: (events: PlayableEvent[]) => Promise<Map<string, AudioBuffer>>,
): Promise<Blob> {
  const buffers = await loadAll(timeline.events);

  // Drum hits ring for their whole sample, so the recording runs until the
  // last sound has died out, not just to the end of the timeline.
  const endMs = timeline.events.reduce((end, e) => {
    const buffer = buffers.get(e.sample);
    const length = e.type === 'melody' ? (e.duration ?? 0) : (buffer?.duration ?? 0) * 1000;
    return Math.max(end, e.time + length);
  }, timeline.duration);

  const frames = Math.max(1, Math.ceil(((endMs + 50) / 1000) * SAMPLE_RATE));
  const context = new OfflineAudioContext(CHANNELS, frames, SAMPLE_RATE);
  for (const event of timeline.events) {
    const buffer = buffers.get(event.sample);
    if (buffer) voice(context, context.destination, buffer, event, event.time / 1000);
  }

  const rendered = await context.startRendering();
  return encodeWav(rendered);
}

/** 16-bit PCM WAV. Normalizes down if the mix would clip. */
export function encodeWav(buffer: AudioBuffer): Blob {
  const channels = Array.from({ length: buffer.numberOfChannels }, (_, c) => buffer.getChannelData(c));
  let peak = 0;
  for (const data of channels) {
    for (let i = 0; i < data.length; i++) peak = Math.max(peak, Math.abs(data[i]));
  }
  const gain = peak > PEAK ? PEAK / peak : 1;

  const bytesPerSample = 2;
  const blockAlign = channels.length * bytesPerSample;
  const dataSize = buffer.length * blockAlign;
  const view = new DataView(new ArrayBuffer(44 + dataSize));
  const ascii = (offset: number, text: string) => [...text].forEach((ch, i) => view.setUint8(offset + i, ch.charCodeAt(0)));

  ascii(0, 'RIFF');
  view.setUint32(4, 36 + dataSize, true);
  ascii(8, 'WAVE');
  ascii(12, 'fmt ');
  view.setUint32(16, 16, true); // fmt chunk size
  view.setUint16(20, 1, true); // PCM
  view.setUint16(22, channels.length, true);
  view.setUint32(24, buffer.sampleRate, true);
  view.setUint32(28, buffer.sampleRate * blockAlign, true);
  view.setUint16(32, blockAlign, true);
  view.setUint16(34, bytesPerSample * 8, true);
  ascii(36, 'data');
  view.setUint32(40, dataSize, true);

  let offset = 44;
  for (let i = 0; i < buffer.length; i++) {
    for (const data of channels) {
      const sample = Math.max(-1, Math.min(1, data[i] * gain));
      view.setInt16(offset, sample < 0 ? sample * 0x8000 : sample * 0x7fff, true);
      offset += 2;
    }
  }

  return new Blob([view.buffer], { type: 'audio/wav' });
}

// ---------------------------------------------------------------------------
// MIDI
// ---------------------------------------------------------------------------

const PPQ = 480; // ticks per quarter note
const DRUM_CHANNEL = 9; // MIDI channel 10
const VELOCITY = 100;

const NOTE_OFFSETS: Record<string, number> = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };

/** "C4" -> 60, "F#3" -> 54, "Bb" -> 70 (octave defaults to 4, like the language). */
export function noteToMidi(note: string): number | null {
  const match = /^([A-G])([#b]?)(-?\d+)?$/.exec(note);
  if (!match) return null;
  const [, letter, accidental, octave] = match;
  const midi = (Number(octave ?? 4) + 1) * 12 + NOTE_OFFSETS[letter] + (accidental === '#' ? 1 : accidental === 'b' ? -1 : 0);
  return Math.max(0, Math.min(127, midi));
}

// General MIDI percussion, picked by words in the sample's file name.
const DRUM_NOTES: [RegExp, number][] = [
  [/open.?hat|openhat|hihat.*open|\/open/i, 46],
  [/hat|hh/i, 42],
  [/kick|bd|bass.?drum/i, 36],
  [/rim/i, 37],
  [/clap/i, 39],
  [/snap/i, 39],
  [/snare|sd/i, 38],
  [/tom.?(high|hi)|high.?tom/i, 50],
  [/tom.?(low|lo)|low.?tom/i, 45],
  [/tom/i, 47],
  [/conga.?0?2/i, 63],
  [/conga/i, 64],
  [/crash/i, 49],
  [/ride/i, 51],
  [/cowbell/i, 56],
  [/shaker/i, 70],
];

function drumNote(sample: string, fallback: number): number {
  return DRUM_NOTES.find(([pattern]) => pattern.test(sample))?.[1] ?? fallback;
}

function trackName(sample: string): string {
  const file = decodeURIComponent(sample.split('?')[0].split('/').pop() ?? sample);
  const folder = sample.startsWith('/samples/') ? sample.split('/')[2] : '';
  const name = file.replace(/\.[a-z0-9]+$/i, '').replace(/^local:/, '');
  return folder && !name.includes(folder) ? `${folder} ${name}` : name;
}

/** A Standard MIDI File (format 1): a tempo track, then one track per sample. */
export function buildMidi(timeline: ExportableTimeline): Blob {
  const msPerTick = 60000 / timeline.bpm / PPQ;
  const ticks = (ms: number) => Math.max(0, Math.round(ms / msPerTick));

  const samples = [...new Set(timeline.events.map((e) => e.sample))];
  let nextChannel = 0;
  let nextFallbackDrum = 60; // unknown percussion: bongos, congas, timbales…

  const tracks: number[][] = [tempoTrack(timeline)];
  for (const sample of samples) {
    const events = timeline.events.filter((e) => e.sample === sample);
    const melodic = events.some((e) => e.type === 'melody');

    // Melodic tracks get their own channel (skipping the drum channel).
    let channel = DRUM_CHANNEL;
    if (melodic) {
      channel = nextChannel % 15;
      if (channel >= DRUM_CHANNEL) channel++;
      nextChannel++;
    }
    const fallbackDrum = melodic ? 0 : nextFallbackDrum;
    if (!melodic && DRUM_NOTES.every(([p]) => !p.test(sample))) nextFallbackDrum = Math.min(81, nextFallbackDrum + 1);

    const messages: { tick: number; order: number; bytes: number[] }[] = [];
    for (const e of events) {
      const note = e.type === 'melody' ? noteToMidi(e.note ?? '') : drumNote(sample, fallbackDrum);
      if (note === null) continue;
      const start = ticks(e.time);
      // Drum hits get a short fixed length (a 16th note); notes their own.
      const end = e.type === 'melody' ? Math.max(start + 1, ticks(e.time + (e.duration ?? 0))) : start + PPQ / 4;
      messages.push({ tick: start, order: 1, bytes: [0x90 | channel, note, VELOCITY] });
      messages.push({ tick: end, order: 0, bytes: [0x80 | channel, note, 0] }); // note-offs first at equal ticks
    }
    messages.sort((a, b) => a.tick - b.tick || a.order - b.order);

    const track: number[] = [...metaText(0x03, trackName(sample))];
    if (melodic) track.push(0x00, 0xc0 | channel, 0); // program 0: piano, a neutral default
    let last = 0;
    for (const m of messages) {
      track.push(...varLength(m.tick - last), ...m.bytes);
      last = m.tick;
    }
    tracks.push(track);
  }

  const chunks: number[] = [
    ...ascii('MThd'),
    ...uint32(6),
    ...uint16(1), // format 1
    ...uint16(tracks.length),
    ...uint16(PPQ),
  ];
  for (const track of tracks) {
    const body = [...track, 0x00, 0xff, 0x2f, 0x00]; // end of track
    chunks.push(...ascii('MTrk'), ...uint32(body.length), ...body);
  }
  return new Blob([new Uint8Array(chunks)], { type: 'audio/midi' });
}

function tempoTrack({ bpm, beatsPerBar }: ExportableTimeline): number[] {
  const microsPerQuarter = Math.round(60_000_000 / bpm);
  return [
    ...metaText(0x03, 'SpinScript'),
    0x00, 0xff, 0x51, 0x03, (microsPerQuarter >> 16) & 0xff, (microsPerQuarter >> 8) & 0xff, microsPerQuarter & 0xff,
    // time signature: beatsPerBar / 4, 24 clocks per click, 8 32nds per quarter
    0x00, 0xff, 0x58, 0x04, Math.min(255, beatsPerBar), 2, 24, 8,
  ];
}

function metaText(type: number, text: string): number[] {
  const bytes = [...new TextEncoder().encode(text)];
  return [0x00, 0xff, type, ...varLength(bytes.length), ...bytes];
}

function varLength(value: number): number[] {
  const bytes = [value & 0x7f];
  for (let v = value >> 7; v > 0; v >>= 7) bytes.unshift((v & 0x7f) | 0x80);
  return bytes;
}

const ascii = (text: string) => [...text].map((ch) => ch.charCodeAt(0));
const uint16 = (n: number) => [(n >> 8) & 0xff, n & 0xff];
const uint32 = (n: number) => [(n >>> 24) & 0xff, (n >> 16) & 0xff, (n >> 8) & 0xff, n & 0xff];

// ---------------------------------------------------------------------------

export function download(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10_000);
}

/** "My song!" + part "groove" -> "my-song-groove" */
export function exportName(song: string, part: string | null): string {
  const slug = (text: string) =>
    text
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  return [slug(song) || 'song', part ? slug(part) : ''].filter(Boolean).join('-');
}
