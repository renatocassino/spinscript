// Every sound shipped with the playground (spinscript-web/public). Keep in
// sync with that folder; the docs list these with a copy button.

export interface SampleGroup {
  name: string;
  samples: { label: string; url: string; rootNote?: string }[];
}

const molla = (folder: string, file: string) => `/samples/molla/MOLLA Sounds - ${folder}/MOLLA Sounds - ${file}.wav`;

export const sampleGroups: SampleGroup[] = [
  {
    name: 'Kick',
    samples: ['Clean', 'Clipped', 'Distorted', 'Lo-Fi', 'Punchy', 'Subby'].map((v) => ({
      label: v,
      url: molla('Kick', `Kick - ${v}`),
    })),
  },
  {
    name: 'Snare & clap',
    samples: [
      ...['BoomBap', 'Brega', 'Funk', 'Latin', 'Tight', 'Trap'].map((v) => ({
        label: `Snare ${v}`,
        url: molla('Snare & Clap', `Snare - ${v}`),
      })),
      ...['Dry', 'Wet'].map((v) => ({ label: `Clap ${v}`, url: molla('Snare & Clap', `Clap - ${v}`) })),
    ],
  },
  {
    name: 'Hi-hat',
    samples: [
      ...['01', '02', '03'].map((v) => ({ label: `Closed ${v}`, url: molla('Hi-Hat', `Hihat - ${v}`) })),
      ...['01', '02'].map((v) => ({ label: `Open ${v}`, url: molla('Hi-Hat', `Openhat - ${v}`) })),
    ],
  },
  {
    name: 'Percussion',
    samples: ['Conga 01', 'Conga 02', 'Rimshot', 'Snap', 'Tom High', 'Tom Low'].map((v) => ({
      label: v,
      url: molla('Perc', v),
    })),
  },
  {
    name: 'Tonal (for melodies)',
    samples: [
      { label: 'Piano', url: molla('Tonal', 'Piano (C3)'), rootNote: 'C3' },
      { label: 'Pad', url: molla('Tonal', 'Pad (C3)'), rootNote: 'C3' },
      { label: 'Phonk lead', url: molla('Tonal', 'Lead (C3) Phonk'), rootNote: 'C3' },
      { label: 'Grand piano', url: '/PIANO-C3.wav', rootNote: 'C3' },
      { label: '808 clean', url: molla('808 Sub', '808 - Clean (A)'), rootNote: 'A1' },
      { label: '808 distorted', url: molla('808 Sub', '808 - Distoted (A)'), rootNote: 'A1' },
    ],
  },
];
