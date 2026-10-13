import { LitElement, css, html } from 'lit';

import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/copy-button/copy-button.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';

import { LOCAL_PREFIX, soundVariableName, type SoundInfo } from '../lib/sound-store';

export function soundLine(sound: SoundInfo): string {
  return `@${soundVariableName(sound.name)} = "${LOCAL_PREFIX}${sound.name}";`;
}

function formatSize(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

/**
 * "Your sounds": a drop zone for audio files plus the list of sounds saved
 * in this browser. Fires `add-files` (detail: { files: File[] }),
 * `preview-sound`, `insert-sound` and `remove-sound` (detail: { name }).
 */
export class SpinScriptSounds extends LitElement {
  static properties = {
    sounds: { attribute: false },
    busy: { type: Boolean },
    persistent: { type: Boolean },
    dragging: { state: true },
  };

  declare sounds: SoundInfo[];
  declare busy: boolean;
  declare persistent: boolean;
  declare dragging: boolean;

  #dragDepth = 0;

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

    .layout {
      display: grid;
      grid-template-columns: minmax(220px, 1fr) minmax(0, 2fr);
      gap: var(--wa-space-s);
      align-items: start;
    }

    .drop {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--wa-space-2xs);
      min-height: 112px;
      padding: var(--wa-space-m);
      border: 2px dashed var(--wa-color-surface-border);
      border-radius: var(--wa-border-radius-l);
      background: var(--wa-color-surface-default);
      color: var(--wa-color-text-quiet);
      text-align: center;
      font-size: var(--wa-font-size-s);
      cursor: pointer;
      transition:
        border-color var(--wa-transition-fast),
        background var(--wa-transition-fast);
    }

    .drop:hover,
    .drop:focus-visible {
      border-color: var(--wa-color-brand-border-normal);
      outline: none;
    }

    .drop.dragging {
      border-color: var(--wa-color-brand-border-loud);
      background: color-mix(in srgb, var(--wa-color-brand-fill-loud) 10%, var(--wa-color-surface-default));
      color: var(--wa-color-text-normal);
    }

    .drop .icon {
      font-size: 1.6rem;
      line-height: 1;
    }

    .drop strong {
      color: var(--wa-color-text-normal);
    }

    .drop small {
      font-size: var(--wa-font-size-xs);
    }

    input[type='file'] {
      display: none;
    }

    .list {
      display: flex;
      flex-direction: column;
      gap: var(--wa-space-3xs);
    }

    .empty {
      color: var(--wa-color-text-quiet);
      font-size: var(--wa-font-size-s);
      padding: var(--wa-space-s) 0;
      max-width: 48ch;
    }

    .sound {
      display: flex;
      align-items: center;
      gap: var(--wa-space-2xs);
      min-width: 0;
      padding: var(--wa-space-3xs) var(--wa-space-2xs);
      border: 1px solid var(--wa-color-surface-border);
      border-radius: var(--wa-border-radius-m);
      background: var(--wa-color-surface-default);
    }

    .sound code {
      flex: 1;
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      font-family: var(--wa-font-family-code);
      font-size: var(--wa-font-size-xs);
    }

    .sound .meta {
      flex-shrink: 0;
      color: var(--wa-color-text-quiet);
      font-size: var(--wa-font-size-xs);
      font-variant-numeric: tabular-nums;
    }

    .warning {
      margin-top: var(--wa-space-2xs);
      color: var(--wa-color-warning-on-quiet);
      font-size: var(--wa-font-size-xs);
    }

    @media (max-width: 720px) {
      .layout {
        grid-template-columns: minmax(0, 1fr);
      }
    }
  `;

  constructor() {
    super();
    this.sounds = [];
    this.busy = false;
    this.persistent = true;
    this.dragging = false;
  }

  #emit(type: string, detail: unknown): void {
    this.dispatchEvent(new CustomEvent(type, { detail, bubbles: true, composed: true }));
  }

  #browse(): void {
    this.shadowRoot!.querySelector<HTMLInputElement>('input[type="file"]')!.click();
  }

  #handlePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = [...(input.files ?? [])];
    input.value = '';
    if (files.length) this.#emit('add-files', { files });
  }

  #handleDragEnter(event: DragEvent): void {
    if (!event.dataTransfer?.types.includes('Files')) return;
    event.preventDefault();
    this.#dragDepth++;
    this.dragging = true;
  }

  #handleDragOver(event: DragEvent): void {
    if (!event.dataTransfer?.types.includes('Files')) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
  }

  #handleDragLeave(): void {
    this.#dragDepth = Math.max(0, this.#dragDepth - 1);
    if (this.#dragDepth === 0) this.dragging = false;
  }

  #handleDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation(); // the editor also accepts drops; handle it once
    this.#dragDepth = 0;
    this.dragging = false;
    const files = [...(event.dataTransfer?.files ?? [])];
    if (files.length) this.#emit('add-files', { files });
  }

  #handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.#browse();
    }
  }

  override render() {
    return html`
      <h2>Your sounds</h2>
      <div class="layout">
        <div
          class="drop ${this.dragging ? 'dragging' : ''}"
          role="button"
          tabindex="0"
          aria-label="Add audio files: drop them here or press to browse"
          @click=${this.#browse}
          @keydown=${this.#handleKeydown}
          @dragenter=${this.#handleDragEnter}
          @dragover=${this.#handleDragOver}
          @dragleave=${this.#handleDragLeave}
          @drop=${this.#handleDrop}
        >
          ${this.busy ? html`<wa-spinner></wa-spinner>` : html`<span class="icon" aria-hidden="true">🎵</span>`}
          <span><strong>Drop audio files here</strong> or click to browse</span>
          <small>WAV, MP3, OGG… up to 25 MB. Each becomes a variable in your song.</small>
          <input type="file" accept="audio/*" multiple @change=${this.#handlePicked} />
        </div>

        <div class="list">
          ${this.sounds.length === 0
            ? html`<p class="empty">
                Your own samples show up here, saved in this browser. Drop one and it's added to the song as
                <code>@name = "local:name.wav";</code> — use it as a <code>sample</code> like any other sound.
              </p>`
            : this.sounds.map((sound) => {
                const line = soundLine(sound);
                return html`<div class="sound">
                  <wa-button
                    size="small"
                    appearance="plain"
                    aria-label="Listen to ${sound.name}"
                    @click=${() => this.#emit('preview-sound', { name: sound.name })}
                    >▶</wa-button
                  >
                  <code title=${line}>${line}</code>
                  <span class="meta">${sound.duration.toFixed(1)}s · ${formatSize(sound.size)}</span>
                  <wa-button
                    size="small"
                    appearance="outlined"
                    title="Add this variable to the current song"
                    @click=${() => this.#emit('insert-sound', { name: sound.name })}
                    >Insert</wa-button
                  >
                  <wa-copy-button value=${line} copy-label="Copy the variable line" success-label="Copied!"></wa-copy-button>
                  <wa-button
                    size="small"
                    appearance="plain"
                    variant="danger"
                    aria-label="Delete ${sound.name}"
                    @click=${() => this.#emit('remove-sound', { name: sound.name })}
                    >✕</wa-button
                  >
                </div>`;
              })}
          ${this.persistent
            ? null
            : html`<p class="warning">
                This browser isn't letting the site store files, so your sounds will be gone when you close the page.
              </p>`}
        </div>
      </div>
    `;
  }
}

customElements.define('spinscript-sounds', SpinScriptSounds);
