# TODO — Lexer & Parser

Baseado no README atual vs. o que já existe em `SpinScript/src/Lexer` e
`SpinScript/src/Parser`.

## Lexer / Tokenizer

- [ ] **Números decimais.** `swing=0.2` (seção "Loops", modificador do
      `play`) não tokeniza — `AddNumberToken` só lê dígitos; o `.` cai no
      `default` do switch e lança `LexerException`. Precisa suportar
      ponto decimal (e decidir se `NUMBER` vira um único tipo com valor
      fracionário, ou se cria um `FLOAT`/`DECIMAL` separado).
- [ ] **Keyword `free`.** Usada nos patterns melódicos:
      `pattern @baixo (sound=bass, free) { ... }` (seção "Patterns"). Não
      está no dicionário `Keywords` do `Lexer.cs` — hoje viraria `IDENT`.
- [ ] **Corpo de pattern melódico.** As linhas `C2  1  0` (nota, duração,
      início em batidas) misturam letras+dígito (`C2`) com números
      separados por espaço, sem vírgula — bem diferente do corpo
      percussivo (`{ 3, 7, 11, 15 }`). Hoje `C2` tokeniza como `IDENT`
      (letra seguida de dígito), o que provavelmente serve, mas vale
      confirmar que não deveria ser um token de nota dedicado.
- [ ] (Opcional) Revisar `TokenType` — hoje só há `NUMBER`; se decimais
      virarem tipo próprio, os testes de `TokenizeNumbersReturnsNumberToken`
      etc. em `TokenizerTests.cs` precisam de casos novos.

## Parser

- [ ] **`ParsePlay`.** Não existe ainda. Cobre `play @kick;` e
      `play @chord (repeat=2, swing=0.2);` (seções "Loops" e "Song").
      Reaproveita o `ParseParams()` que já existe (parênteses e
      parâmetros opcionais) — só falta o wiring: `Consume(PLAY)` →
      `Consume(REFERENCE)` → `ParseParams()` → `Consume(SEMICOLON)`.
      Precisa de um `PlayNode(string Target, Dictionary<string,string>
      Parameters)` em `Ast.cs`.
- [ ] **`ParseLoop`.** Não existe. Gramática:
      `loop @groove (bars=1) { play @kick; play @snare; ... }` — keyword
      `LOOP`, nome, `ParseParams()` opcional, `{`, lista de statements
      `play` (reaproveitando `ParsePlay`), `}`. Precisa de
      `LoopNode(string Name, Dictionary<string,string> Parameters,
      List<PlayNode> Body)` (ou lista de `AstNode` se o corpo puder ter
      mais tipos de statement no futuro).
- [ ] **`ParseSong`.** Não existe. Gramática: `song { play @refrao; ...
      }` — keyword `SONG`, `{`, lista de `play`, `}`. Mesma ideia do
      `ParseLoop`, sem nome nem parênteses (é singleton por arquivo?
      vale confirmar essa regra).
- [ ] **Ligar os três no dispatcher `Parse()`.** O `switch` em
      `Parser.cs` só tem `case`s pra `REFERENCE`, `PATTERN` e `EOF` — os
      tokens `LOOP`, `PLAY` e `SONG` caem no `default` e explodem
      `ParserException`.
- [ ] **Corpo melódico do pattern (`free`).** `ParseSteps()` só lê
      `NUMBER` separados por vírgula — não cobre o formato
      `NOTA DURACAO INICIO` por linha usado quando o pattern é `free`.
      Depende de decimais no lexer (duração/início podem ser fracionários
      dependendo do exemplo) e de `free` virar keyword primeiro.
- [ ] **Padronizar `@` em declaração de pattern/loop.** O README se
      contradiz: seção "Patterns" mostra `pattern @kick (...)`, mas
      "Exemplo completo" mostra `pattern kick (...)` (sem `@`) — mesma
      coisa pra `loop`. `ParsePattern` hoje exige `Consume(REFERENCE)`
      (ou seja, exige `@`). Decidir qual é a regra oficial e atualizar
      README + parser pra baterem.
- [ ] (Cleanup) `public int index` em `Parser.cs` dispara aviso do
      analyzer (`CA1051`, campo público visível). Como `Peek`/`Check`/
      `Match`/`Consume` já encapsulam todo acesso, dá pra deixar
      `index` privado.
