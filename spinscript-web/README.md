# spinscript-web

SpinScript playground UI: an Astro site with a Lit web component
(`<spinscript-editor>`) that loads the SpinScript.Wasm .NET runtime and
plays the resulting events with the Web Audio API. Same page as
`SpinScript.Wasm/index.html`, rebuilt on Astro + Lit instead of plain
HTML/JS.

## Project structure

```text
src/
  components/
    spinscript-editor.ts   # Lit component: editor, highlighting, WASM, audio
  pages/
    index.astro             # mounts <spinscript-editor>
scripts/
  sync-wasm.mjs              # copies SpinScript.Wasm's build output into public/wasm
```

`public/wasm/` is generated, not committed — it's copied in from
`../SpinScript.Wasm/bin/Debug/net10.0/browser-wasm/AppBundle/_framework`.

## Commands

| Command             | Action                                                          |
| :------------------- | :--------------------------------------------------------------- |
| `dotnet build` (in `../SpinScript.Wasm`) | Build the .NET WASM runtime — do this first |
| `npm install`         | Install dependencies                                             |
| `npm run dev`         | Sync the WASM runtime, then start the dev server at `localhost:4321` |
| `npm run build`       | Sync the WASM runtime, then build the production site to `./dist/` |
| `npm run preview`     | Preview a production build locally                               |
| `npm run sync-wasm`   | Just copy the WASM runtime into `public/wasm` (run after rebuilding the .NET project) |

`dev` and `build` run `sync-wasm` automatically, but if you rebuild the
.NET project while the dev server is already running, re-run
`npm run sync-wasm` yourself to pick up the change.
