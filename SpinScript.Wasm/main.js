import { dotnet } from './_framework/dotnet.js';

globalThis.SpinScriptReady = (async () => {
  const { getAssemblyExports } = await dotnet.create();
  const exports = await getAssemblyExports('SpinScript.Wasm.dll');

  globalThis.SpinScript = exports.SpinScript.Wasm.Exports;
  return globalThis.SpinScript;
})().catch((error) => {
  console.error('Failed to load SpinScript wasm module:', error);
  throw error;
});

let isPlaying = true;

const textarea = document.querySelector('#spincode');
const errorInput = document.getElementById('error');
const playButton = document.getElementById('play');

document.addEventListener('DOMContentLoaded', () => {
    const savedCode = localStorage.getItem('spincode');
    if (savedCode) {
    textarea.value = savedCode;
    try {
        SpinScript.Parse(savedCode);
        errorInput.value = '';
    } catch (e) {
        errorInput.value = e.message;
    }
    }
});

textarea.addEventListener('change', () => {
    errorInput.value = '';
    try {
    localStorage.setItem('spincode', textarea.value);
    SpinScript.Parse(textarea.value);
    errorInput.value = '';
    } catch (e) {
    errorInput.value = e.message;
    }
});

const audioContext = new (window.AudioContext || window.webkitAudioContext)();
const sampleBufferCache = new Map();

async function loadSample(url) {
    if (sampleBufferCache.has(url)) {
        return sampleBufferCache.get(url);
    }

    const response = await fetch(url);
    const arrayBuffer = await response.arrayBuffer();
    const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);

    sampleBufferCache.set(url, audioBuffer);
    return audioBuffer;
}

async function loadSamples(urls) {
    const buffers = await Promise.all(urls.map(loadSample));
    return new Map(urls.map((url, i) => [url, buffers[i]]));
}

// Schedules `buffer` to start playing `delayMs` milliseconds from now (uses
// the AudioContext clock, not setTimeout, so timing stays sample-accurate).
function playSample(buffer, delayMs = 0) {
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    source.connect(audioContext.destination);
    source.start(audioContext.currentTime + delayMs / 1000);
    return source;
}

// Example: schedule every event from the interpreter result. `event.time`
// already comes from SpinScript in milliseconds, so it's passed straight in.
// for (const event of result.events) {
//     const buffer = buffers.get(event.sample);
//     playSample(buffer, event.time);
// }

playButton.addEventListener('click', async () => {
    errorInput.value = '';
    try {
        const result = JSON.parse(SpinScript.Interpret(textarea.value));
        console.log(result);

        const uniqueSamples = Object.keys(result.events.reduce((accum, curr) => (
            {...accum, [curr.sample]: true}
        ), {}));

        const buffers = await loadSamples(uniqueSamples);
        console.log('Samples loaded (not playing yet):', buffers);

        isPlaying = true;
        let cloneEvents = [...result.events];

        const startAudioTime = audioContext.currentTime;
        const lookaheadMs = 300;
        const schedulerIntervalMs = 25;

        function playNextEvents() {
            const now = audioContext.currentTime;

            cloneEvents = cloneEvents.filter((event) => {
                const { time, sample } = event;
                const scheduledAt = startAudioTime + time / 1000;
                const timeToStart = (scheduledAt - now) * 1000;

                if (timeToStart < lookaheadMs) {
                    const buffer = buffers.get(sample);
                    if (buffer) {
                        playSample(buffer, Math.max(timeToStart, 0));
                    }
                    return false;
                }

                return true;
            });

            if (cloneEvents.length === 0 || !isPlaying) {
                isPlaying = false;
                return;
            }

            setTimeout(() => {
                if (!isPlaying) return;
                playNextEvents();
            }, schedulerIntervalMs);
        }
        playNextEvents();
    } catch (e) {
    errorInput.value = e.message;
    }
});