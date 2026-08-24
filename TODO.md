# TODO — Lexer & Parser

Baseado no README atual vs. o que já existe em `SpinScript/src/Lexer` e
`SpinScript/src/Parser`.

## Lexer / Tokenizer

- [x] **Números decimais.** `swing=0.2` (seção "Loops", modificador do
      `play`) não tokeniza — `AddNumberToken` só lê dígitos; o `.` cai no
      `default` do switch e lança `LexerException`. Precisa suportar
      ponto decimal (e decidir se `NUMBER` vira um único tipo com valor
      fracionário, ou se cria um `FLOAT`/`DECIMAL` separado).

## Parser

- [x] **`ParsePlay`.** Não existe ainda. Cobre `play @kick;` e
      `play @chord (repeat=2, swing=0.2);` (seções "Loops" e "Song").
      Reaproveita o `ParseParams()` que já existe (parênteses e
      parâmetros opcionais) — só falta o wiring: `Consume(PLAY)` →
      `Consume(REFERENCE)` → `ParseParams()` → `Consume(SEMICOLON)`.
      Precisa de um `PlayNode(string Target, Dictionary<string,string>
      Parameters)` em `Ast.cs`.
- [x] **`ParseLoop`.** Não existe. Gramática:
      `loop @groove (bars=1) { play @kick; play @snare; ... }` — keyword
      `LOOP`, nome, `ParseParams()` opcional, `{`, lista de statements
      `play` (reaproveitando `ParsePlay`), `}`. Precisa de
      `LoopNode(string Name, Dictionary<string,string> Parameters,
      List<PlayNode> Body)` (ou lista de `AstNode` se o corpo puder ter
      mais tipos de statement no futuro).
- [x] **`ParseSong`.** Não existe. Gramática: `song { play @refrao; ...
      }` — keyword `SONG`, `{`, lista de `play`, `}`. Mesma ideia do
      `ParseLoop`, sem nome nem parênteses (é singleton por arquivo?
      vale confirmar essa regra).
- [x] **Ligar os três no dispatcher `Parse()`.** O `switch` em
      `Parser.cs` só tem `case`s pra `REFERENCE`, `beat` e `EOF` — os
      tokens `LOOP`, `PLAY` e `SONG` caem no `default` e explodem
      `ParserException`.
- [x] **Corpo melódico do beat (`free`).** `ParseSteps()` só lê
      `NUMBER` separados por vírgula — não cobre o formato
      `NOTA DURACAO INICIO` por linha usado quando o beat é `free`.
      Depende de decimais no lexer (duração/início podem ser fracionários
      dependendo do exemplo) e de `free` virar keyword primeiro.
- [x] **Padronizar `@` em declaração de beat/loop.** O README se
      contradiz: seção "beats" mostra `beat @kick (...)`, mas
      "Exemplo completo" mostra `beat kick (...)` (sem `@`) — mesma
      coisa pra `loop`. `Parsebeat` hoje exige `Consume(REFERENCE)`
      (ou seja, exige `@`). Decidir qual é a regra oficial e atualizar
      README + parser pra baterem.
- [x] (Cleanup) `public int index` em `Parser.cs` dispara aviso do
      analyzer (`CA1051`, campo público visível). Como `Peek`/`Check`/
      `Match`/`Consume` já encapsulam todo acesso, dá pra deixar
      `index` privado.
