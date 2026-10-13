// Every sound shipped with the playground, in public/samples/<category>/.
// Keep in sync with that folder; the docs list these with a copy button.
// Tonal samples carry the note they were recorded at in their name
// (piano-c3, 808/clean-a): that's the rootNote a melody needs.

export interface Sample {
  label: string;
  url: string;
  /** Suggested variable name for the docs' copy button (without '@'). */
  variable: string;
  rootNote?: string;
}

export interface SampleGroup {
  name: string;
  folder: string;
  samples: Sample[];
}

// "snare", "boom-bap" -> "snareBoomBap"
function camel(...parts: string[]): string {
  const words = parts.join('-').split(/[^A-Za-z0-9]+/).filter(Boolean);
  const name = words.map((w, i) => (i === 0 ? w.toLowerCase() : w[0].toUpperCase() + w.slice(1).toLowerCase())).join('');
  return /^[A-Za-z]/.test(name) ? name : `s${name}`;
}

const group = (name: string, folder: string, files: string[]): SampleGroup => ({
  name,
  folder,
  samples: files.map((file) => ({ label: file, url: `/samples/${folder}/${file}.wav`, variable: camel(folder, file) })),
});

const tonal = (folder: string, file: string, variable: string, rootNote: string): Sample => ({
  label: file,
  url: `/samples/${folder}/${file}.wav`,
  variable,
  rootNote,
});

export const sampleGroups: SampleGroup[] = [
  group('Kick', 'kick', ['clean', 'clipped', 'distorted', 'lo-fi', 'punchy', 'subby']),
  group('Snare', 'snare', ['boom-bap', 'brega', 'funk', 'latin', 'tight', 'trap']),
  group('Clap', 'clap', ['dry', 'wet']),
  group('Hi-hat', 'hihat', ['closed-01', 'closed-02', 'closed-03', 'open-01', 'open-02']),
  group('Percussion', 'perc', ['conga-01', 'conga-02', 'rimshot', 'snap', 'tom-high', 'tom-low']),
  {
    name: 'Tonal (for melodies)',
    folder: 'tonal',
    samples: [
      tonal('tonal', 'piano-c3', 'piano', 'C3'),
      tonal('tonal', 'grand-piano-c3', 'grandPiano', 'C3'),
      tonal('tonal', 'pad-c3', 'pad', 'C3'),
      tonal('tonal', 'lead-phonk-c3', 'lead', 'C3'),
    ],
  },
  {
    name: '808 (for basslines)',
    folder: '808',
    samples: [tonal('808', 'clean-a', 'sub', 'A1'), tonal('808', 'distorted-a', 'subDistorted', 'A1')],
  },
];
