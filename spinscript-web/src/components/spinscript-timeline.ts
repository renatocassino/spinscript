import { LitElement, css, html, svg } from 'lit';

import type { PlayableEvent, Player } from '../lib/player';

// One color per lane (sample), cycling. Web Awesome palette tokens, so they
// follow light/dark mode.
const LANE_COLORS = ['blue', 'orange', 'green', 'purple', 'red', 'cyan', 'yellow', 'pink', 'indigo'].map(
  (hue) => `var(--wa-color-${hue}-60, var(--wa-color-brand-fill-loud))`,
);

const VIEW_WIDTH = 1000;

export function formatTime(ms: number): string {
  const totalSeconds = Math.max(0, ms) / 1000;
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds - minutes * 60;
  return `${minutes}:${seconds.toFixed(1).padStart(4, '0')}`;
}

function sampleLabel(url: string): string {
  const file = decodeURIComponent(url.split('?')[0].split('/').pop() ?? url);
  return file.replace(/\.(wav|mp3|ogg|flac)$/i, '').replace(/^MOLLA Sounds - /, '');
}

/**
 * The player bar: current / total time and an overview of the timeline
 * (one lane per sample; drum hits as ticks, notes as bars). Clicking or
 * dragging on it fires `seek` (detail: { ms }).
 *
 * While playing, the playhead is moved straight on the DOM every animation
 * frame instead of through a Lit re-render, so it stays cheap.
 */
export class SpinScriptTimeline extends LitElement {
  static properties = {
    events: { attribute: false },
    duration: { type: Number },
    label: { type: String },
    player: { attribute: false },
    hoverMs: { state: true },
  };

  declare events: PlayableEvent[];
  declare duration: number;
  declare label: string;
  declare player: Player | null;
  declare hoverMs: number | null;

  #frame: number | null = null;
  #downX: number | null = null;
  #onStateChange = (): void => this.#syncLoop();
  #observed: Player | null = null;

  static styles = css`
    :host {
      display: block;
      font-family: var(--wa-font-family-body);
    }

    .head {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: var(--wa-space-s);
      margin-bottom: var(--wa-space-2xs);
      font-size: var(--wa-font-size-s);
    }

    .label {
      color: var(--wa-color-text-quiet);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .times {
      font-family: var(--wa-font-family-code);
      font-variant-numeric: tabular-nums;
      flex-shrink: 0;
    }

    .times .total {
      color: var(--wa-color-text-quiet);
    }

    .track {
      position: relative;
      height: 56px;
      border: 1px solid var(--wa-color-surface-border);
      border-radius: var(--wa-border-radius-m);
      background: var(--wa-color-surface-lowered);
      overflow: hidden;
      cursor: pointer;
      touch-action: none;
    }

    .track.empty {
      cursor: default;
      display: grid;
      place-items: center;
      color: var(--wa-color-text-quiet);
      font-size: var(--wa-font-size-s);
    }

    svg {
      position: absolute;
      inset: 4px 0;
      width: 100%;
      height: calc(100% - 8px);
    }

    .played {
      position: absolute;
      top: 0;
      bottom: 0;
      left: 0;
      width: 0;
      background: color-mix(in srgb, var(--wa-color-brand-fill-loud) 14%, transparent);
      pointer-events: none;
    }

    .playhead,
    .hover {
      position: absolute;
      top: 0;
      bottom: 0;
      width: 2px;
      margin-left: -1px;
      pointer-events: none;
    }

    .playhead {
      left: 0;
      background: var(--wa-color-brand-fill-loud);
      display: none;
    }

    :host([playing]) .playhead {
      display: block;
    }

    .hover {
      background: var(--wa-color-text-quiet);
      opacity: 0.5;
    }

    .hover-time {
      position: absolute;
      top: 2px;
      padding: 0 var(--wa-space-3xs);
      font-family: var(--wa-font-family-code);
      font-size: var(--wa-font-size-2xs);
      background: var(--wa-color-surface-default);
      border-radius: var(--wa-border-radius-s);
      transform: translateX(-50%);
      pointer-events: none;
      white-space: nowrap;
    }

    .scale {
      display: flex;
      justify-content: space-between;
      margin-top: var(--wa-space-3xs);
      font-family: var(--wa-font-family-code);
      font-size: var(--wa-font-size-2xs);
      color: var(--wa-color-text-quiet);
    }
  `;

  constructor() {
    super();
    this.events = [];
    this.duration = 0;
    this.label = '';
    this.player = null;
    this.hoverMs = null;
  }

  override disconnectedCallback(): void {
    super.disconnectedCallback();
    this.#observe(null);
    this.#stopLoop();
  }

  override updated(changed: Map<string, unknown>): void {
    if (changed.has('player')) this.#observe(this.player);
    this.#paint();
  }

  #observe(player: Player | null): void {
    this.#observed?.removeEventListener('statechange', this.#onStateChange);
    this.#observed = player;
    player?.addEventListener('statechange', this.#onStateChange);
    this.#syncLoop();
  }

  #syncLoop(): void {
    const playing = this.player?.playing ?? false;
    this.toggleAttribute('playing', playing);
    if (playing && this.#frame === null) {
      const step = (): void => {
        this.#paint();
        this.#frame = requestAnimationFrame(step);
      };
      this.#frame = requestAnimationFrame(step);
    } else if (!playing) {
      this.#stopLoop();
      this.#paint();
    }
  }

  #stopLoop(): void {
    if (this.#frame !== null) cancelAnimationFrame(this.#frame);
    this.#frame = null;
  }

  // Moves the playhead and the current time without a Lit render.
  #paint(): void {
    const root = this.shadowRoot;
    if (!root) return;
    const position = this.player?.playing ? this.player.position : 0;
    const ratio = this.duration > 0 ? Math.min(1, position / this.duration) : 0;
    const percent = `${ratio * 100}%`;
    const playhead = root.querySelector<HTMLElement>('.playhead');
    const played = root.querySelector<HTMLElement>('.played');
    const current = root.querySelector<HTMLElement>('.current');
    if (playhead) playhead.style.left = percent;
    if (played) played.style.width = this.player?.playing ? percent : '0';
    if (current) current.textContent = formatTime(position);
  }

  #msAt(event: PointerEvent): number {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const ratio = Math.min(1, Math.max(0, (event.clientX - rect.left) / rect.width));
    return ratio * this.duration;
  }

  #handlePointerDown(event: PointerEvent): void {
    if (this.duration <= 0) return;
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    this.#downX = event.clientX;
    this.#seek(this.#msAt(event));
  }

  #handlePointerMove(event: PointerEvent): void {
    if (this.duration <= 0) return;
    this.hoverMs = this.#msAt(event);
  }

  #handlePointerUp(event: PointerEvent): void {
    const target = event.currentTarget as HTMLElement;
    if (target.hasPointerCapture(event.pointerId)) target.releasePointerCapture(event.pointerId);
    // A drag restarts from wherever it was let go.
    if (this.#downX !== null && Math.abs(event.clientX - this.#downX) > 4) this.#seek(this.#msAt(event));
    this.#downX = null;
  }

  #seek(ms: number): void {
    this.dispatchEvent(new CustomEvent('seek', { detail: { ms }, bubbles: true, composed: true }));
  }

  #handleKeydown(event: KeyboardEvent): void {
    if (this.duration <= 0) return;
    const step = event.shiftKey ? 10000 : 2000;
    const position = this.player?.playing ? this.player.position : 0;
    if (event.key === 'ArrowRight') this.#seek(Math.min(this.duration, position + step));
    else if (event.key === 'ArrowLeft') this.#seek(Math.max(0, position - step));
    else if (event.key === 'Home') this.#seek(0);
    else return;
    event.preventDefault();
  }

  #lanes() {
    const lanes = [...new Set(this.events.map((e) => e.sample))];
    const laneHeight = 100 / Math.max(1, lanes.length);
    const scale = VIEW_WIDTH / this.duration;

    return svg`<svg viewBox="0 0 ${VIEW_WIDTH} 100" preserveAspectRatio="none" aria-hidden="true">
      ${lanes.map((sample, lane) => {
        const color = LANE_COLORS[lane % LANE_COLORS.length];
        const y = lane * laneHeight + laneHeight * 0.15;
        const h = laneHeight * 0.7;
        return svg`<g fill=${color}>
          <title>${sampleLabel(sample)}</title>
          ${this.events
            .filter((e) => e.sample === sample)
            .map((e) => {
              const width = e.type === 'melody' ? Math.max(1.5, (e.duration ?? 0) * scale - 0.5) : 1.5;
              return svg`<rect x=${e.time * scale} y=${y} width=${width} height=${h} rx="0.5"></rect>`;
            })}
        </g>`;
      })}
    </svg>`;
  }

  override render() {
    const empty = this.duration <= 0 || this.events.length === 0;
    const hover =
      this.hoverMs !== null && !empty
        ? html`<div class="hover" style="left:${(this.hoverMs / this.duration) * 100}%"></div>
            <div class="hover-time" style="left:${(this.hoverMs / this.duration) * 100}%">${formatTime(this.hoverMs)}</div>`
        : null;

    return html`
      <div class="head">
        <span class="label">${this.label}</span>
        <span class="times"><span class="current">0:00.0</span> <span class="total">/ ${formatTime(this.duration)}</span></span>
      </div>
      ${empty
        ? html`<div class="track empty">Nothing to play yet</div>`
        : html`<div
            class="track"
            role="slider"
            tabindex="0"
            aria-label="Playback position — click to play from there"
            aria-valuemin="0"
            aria-valuemax=${Math.round(this.duration / 1000)}
            @pointerdown=${this.#handlePointerDown}
            @pointermove=${this.#handlePointerMove}
            @pointerup=${this.#handlePointerUp}
            @pointerleave=${() => (this.hoverMs = null)}
            @keydown=${this.#handleKeydown}
          >
            ${this.#lanes()}
            <div class="played"></div>
            <div class="playhead"></div>
            ${hover}
          </div>`}
      <div class="scale"><span>0:00.0</span><span>${formatTime(this.duration)}</span></div>
    `;
  }
}

customElements.define('spinscript-timeline', SpinScriptTimeline);
