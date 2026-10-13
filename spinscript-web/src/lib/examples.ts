// The example songs are plain .spin files in src/examples (a C# test makes
// sure each one compiles without errors or warnings). The first comment
// line is the title, the following comment lines the description.

export interface Example {
  id: string;
  title: string;
  description: string;
  code: string;
}

const files = import.meta.glob('../examples/*.spin', { query: '?raw', import: 'default', eager: true }) as Record<
  string,
  string
>;

function describe(path: string, code: string): Example {
  const id = path.split('/').pop()!.replace(/\.spin$/, '');
  const header: string[] = [];
  for (const line of code.split('\n')) {
    const match = /^\s*\/\/\s?(.*)$/.exec(line);
    if (!match) break;
    header.push(match[1].trim());
  }
  const [title = id, ...rest] = header;
  return { id, title, description: rest.filter(Boolean).join(' '), code };
}

export const examples: Example[] = Object.entries(files)
  .sort(([a], [b]) => a.localeCompare(b))
  .map(([path, code]) => describe(path, code));
