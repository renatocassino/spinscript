import { css } from 'lit';

// Mirrors spinscript-vscode/syntaxes/spinscript.tmLanguage.json, in the same
// precedence order (comments/strings/numbers first, punctuation last).
const TOKEN_REGEX = new RegExp(
  [
    String.raw`(?<comment>/\*[\s\S]*?\*/|//.*)`,
    String.raw`(?<string>"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')`,
    // A named chord (C4:maj7, G3:7). Before `number` so the quality's
    // digits aren't highlighted on their own. Mirrors Lexer.TryAddChord.
    String.raw`(?<chord>\b[A-G][#b]?[0-9]?:[A-Za-z0-9#]+)`,
    // Must come before `number` so a fraction like `1/4` is captured whole
    // instead of just matching its numerator.
    String.raw`(?<fraction>\b[0-9]+/[0-9]+\b)`,
    String.raw`(?<number>\b[0-9]+(?:\.[0-9]+)?\b)`,
    String.raw`(?<keyword>\b(?:song|loop|beat|melody|play)\b)`,
    // A letter A-G, optionally followed by an octave digit (A4), an
    // accidental (C#, Db), or both (Eb8, F#9). Mirrors Lexer.CheckIsNote.
    // A lone '_' is a melody rest; inside a name (@main_groove) it isn't.
    String.raw`(?<rest>(?<![A-Za-z0-9_@])_(?![A-Za-z0-9_]))`,
    String.raw`(?<note>\b[A-G](?:[#b][0-9]?|[0-9])?\b)`,
    // A beat grid like x...x..., x|x|x|x, x...|x..x.|.x.xx: a lowercase 'x'
    // followed by any run of 'x', '.' and '|' with no spaces. Mirrors
    // Lexer.CheckIsPatternGrid — a digit or other letter in the run falls
    // back to an identifier/note instead, hence the trailing lookahead.
    String.raw`(?<patternGrid>\bx[x.|]*(?![A-Za-z0-9_]))`,
    String.raw`(?<variable>@[A-Za-z][A-Za-z0-9_]*)`,
    String.raw`(?<parameter>\b[A-Za-z_][A-Za-z0-9_]*\b(?=\s*=))`,
  ].join('|'),
  'g',
);

export function escapeHtml(text: string): string {
  return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

export function highlightSpinScript(code: string): string {
  let out = '';
  let lastIndex = 0;

  for (const match of code.matchAll(TOKEN_REGEX)) {
    out += escapeHtml(code.slice(lastIndex, match.index));

    const tokenType = Object.keys(match.groups!).find((key) => match.groups![key] !== undefined);
    out += `<span class="tok-${tokenType}">${escapeHtml(match[0])}</span>`;

    lastIndex = match.index! + match[0].length;
  }

  out += escapeHtml(code.slice(lastIndex));

  // Keeps the overlay's scroll height matching the textarea's when the
  // code ends with a trailing newline.
  return out + '\n';
}

// One number per logical line, matching the textarea's line count. Doesn't
// account for visually wrapped lines (white-space: pre-wrap), so numbers can
// drift from the wrapped row once a line is long enough to wrap — an
// accepted trade-off for a plain <textarea>-based editor.
export function lineNumbers(code: string): string {
  const count = code.split('\n').length;
  return Array.from({ length: count }, (_, i) => i + 1).join('\n');
}

// Token colors come from --spin-tok-* custom properties (set per theme in
// index.astro), which inherit into every shadow root that includes this.
export const tokenStyles = css`
  .tok-comment {
    color: var(--spin-tok-comment);
    font-style: italic;
  }
  .tok-string {
    color: var(--spin-tok-string);
  }
  .tok-number,
  .tok-fraction {
    color: var(--spin-tok-number);
  }
  .tok-keyword {
    color: var(--spin-tok-keyword);
    font-weight: bold;
  }
  .tok-note,
  .tok-chord {
    color: var(--spin-tok-note);
  }
  .tok-rest {
    color: var(--spin-tok-note);
    opacity: 0.6;
  }
  .tok-patternGrid {
    color: var(--spin-tok-grid);
  }
  .tok-variable {
    color: var(--spin-tok-variable);
  }
  .tok-parameter {
    color: var(--spin-tok-parameter);
  }
`;
