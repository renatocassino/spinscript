// Where each bundled sample used to live, and where it lives now. Songs
// saved in a visitor's browser before the samples were reorganized still
// point at the old URLs; loadLibrary() rewrites them with this table.
export const RENAMED_SAMPLES: [from: string, to: string][] = [
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Clean.wav", "/samples/kick/clean.wav"],
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Clipped.wav", "/samples/kick/clipped.wav"],
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Distorted.wav", "/samples/kick/distorted.wav"],
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Lo-Fi.wav", "/samples/kick/lo-fi.wav"],
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Punchy.wav", "/samples/kick/punchy.wav"],
  ["/samples/molla/MOLLA Sounds - Kick/MOLLA Sounds - Kick - Subby.wav", "/samples/kick/subby.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - BoomBap.wav", "/samples/snare/boom-bap.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Brega.wav", "/samples/snare/brega.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Funk.wav", "/samples/snare/funk.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Latin.wav", "/samples/snare/latin.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Tight.wav", "/samples/snare/tight.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Snare - Trap.wav", "/samples/snare/trap.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Clap - Dry.wav", "/samples/clap/dry.wav"],
  ["/samples/molla/MOLLA Sounds - Snare & Clap/MOLLA Sounds - Clap - Wet.wav", "/samples/clap/wet.wav"],
  ["/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 01.wav", "/samples/hihat/closed-01.wav"],
  ["/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 02.wav", "/samples/hihat/closed-02.wav"],
  ["/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Hihat - 03.wav", "/samples/hihat/closed-03.wav"],
  ["/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Openhat - 01.wav", "/samples/hihat/open-01.wav"],
  ["/samples/molla/MOLLA Sounds - Hi-Hat/MOLLA Sounds - Openhat - 02.wav", "/samples/hihat/open-02.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Conga 01.wav", "/samples/perc/conga-01.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Conga 02.wav", "/samples/perc/conga-02.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Rimshot.wav", "/samples/perc/rimshot.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Snap.wav", "/samples/perc/snap.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Tom High.wav", "/samples/perc/tom-high.wav"],
  ["/samples/molla/MOLLA Sounds - Perc/MOLLA Sounds - Tom Low.wav", "/samples/perc/tom-low.wav"],
  ["/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Piano (C3).wav", "/samples/tonal/piano-c3.wav"],
  ["/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Pad (C3).wav", "/samples/tonal/pad-c3.wav"],
  ["/samples/molla/MOLLA Sounds - Tonal/MOLLA Sounds - Lead (C3) Phonk.wav", "/samples/tonal/lead-phonk-c3.wav"],
  ["/PIANO-C3.wav", "/samples/tonal/grand-piano-c3.wav"],
  ["/samples/molla/MOLLA Sounds - 808 Sub/MOLLA Sounds - 808 - Clean (A).wav", "/samples/808/clean-a.wav"],
  ["/samples/molla/MOLLA Sounds - 808 Sub/MOLLA Sounds - 808 - Distoted (A).wav", "/samples/808/distorted-a.wav"],
];

export function migrateSampleUrls(code: string): string {
  let migrated = code;
  for (const [from, to] of RENAMED_SAMPLES) {
    migrated = migrated.split(from).join(to);
  }
  return migrated;
}
