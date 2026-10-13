import { LitElement, css, html } from 'lit';

import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/tag/tag.js';

export interface Part {
  name: string;
  kind: 'beat' | 'melody' | 'loop';
  detail: string;
  line: number;
}

const KIND_VARIANT = { beat: 'warning', melody: 'success', loop: 'brand' } as const;

/**
 * One card per top-level beat, melody and loop, each with its own play
 * button. Fires `play-part` (detail: { name }) and `stop-part`.
 */
export class SpinScriptParts extends LitElement {
  static properties = {
    parts: { attribute: false },
    playing: { type: String },
    disabled: { type: Boolean },
  };

  declare parts: Part[];
  declare playing: string | null;
  declare disabled: boolean;

  static styles = css`
    :host {
      display: block;
      font-family: var(--wa-font-family-body);
    }

    h2 {
      margin: 0 0 var(--wa-space-xs);
      font-size: var(--wa-font-size-s);
      font-weight: var(--wa-font-weight-semibold);
      color: var(--wa-color-text-quiet);
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(215px, 1fr));
      gap: var(--wa-space-xs);
    }

    .part {
      display: flex;
      align-items: center;
      gap: var(--wa-space-xs);
      min-width: 0;
      padding: var(--wa-space-xs) var(--wa-space-xs) var(--wa-space-xs) var(--wa-space-s);
      border: 1px solid var(--wa-color-surface-border);
      border-radius: var(--wa-border-radius-m);
      background: var(--wa-color-surface-default);
    }

    .part.playing {
      border-color: var(--wa-color-brand-border-loud);
      box-shadow: 0 0 0 1px var(--wa-color-brand-border-loud);
    }

    .info {
      flex: 1;
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: var(--wa-space-3xs);
    }

    .name {
      font-family: var(--wa-font-family-code);
      font-size: var(--wa-font-size-s);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .meta {
      display: flex;
      align-items: center;
      gap: var(--wa-space-2xs);
      font-size: var(--wa-font-size-xs);
      color: var(--wa-color-text-quiet);
      white-space: nowrap;
      overflow: hidden;
    }

    wa-tag {
      font-size: var(--wa-font-size-2xs);
    }

    .play {
      flex-shrink: 0;
    }
  `;

  constructor() {
    super();
    this.parts = [];
    this.playing = null;
    this.disabled = false;
  }

  #toggle(name: string): void {
    const type = this.playing === name ? 'stop-part' : 'play-part';
    this.dispatchEvent(new CustomEvent(type, { detail: { name }, bubbles: true, composed: true }));
  }

  override render() {
    if (this.parts.length === 0) return null;

    return html`
      <h2>Parts</h2>
      <div class="grid">
        ${this.parts.map((part) => {
          const playing = this.playing === part.name;
          return html`<div class="part ${playing ? 'playing' : ''}" title="Line ${part.line + 1}">
            <div class="info">
              <span class="name">@${part.name}</span>
              <span class="meta">
                <wa-tag size="small" variant=${KIND_VARIANT[part.kind]} appearance="filled-outlined" pill>${part.kind}</wa-tag>
                ${part.detail}
              </span>
            </div>
            <wa-button
              class="play"
              size="small"
              appearance=${playing ? 'filled' : 'outlined'}
              variant=${playing ? 'brand' : 'neutral'}
              ?disabled=${this.disabled && !playing}
              aria-label=${playing ? `Stop @${part.name}` : `Play only @${part.name}`}
              @click=${() => this.#toggle(part.name)}
            >
              ${playing ? '■' : '▶'}
            </wa-button>
          </div>`;
        })}
      </div>
    `;
  }
}

customElements.define('spinscript-parts', SpinScriptParts);
