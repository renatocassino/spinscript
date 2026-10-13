# TODO — Roadmap da linguagem

O TODO anterior (lexer & parser: decimais, `play`, `loop`, `song`, corpo
melódico, `@` nas declarações) foi todo concluído — está no histórico do
git. Este aqui é o que a SpinScript *poderia* ter daqui pra frente,
agrupado por área e mais ou menos em ordem de prioridade dentro de cada
grupo.

## Correções / dívidas conhecidas

- [x] **Grid pattern 0-indexado vs. lista 1-indexada.** O compilador
      subtrai 1 dos dois formatos, então `{ x... }` dispara um step antes
      (e o primeiro `x` cai *antes* do compasso). Normalizar no parser ou
      no compilador e cobrir com teste.
- [ ] **Parâmetros aceitos mas ignorados.** `swing`, `instrument` e os
      parâmetros por nota de `melody` parseiam, mas o compilador não lê.
      Ou implementar, ou emitir *warning* de "parâmetro desconhecido".
- [ ] **`SoundEvent.Velocity`** existe no evento mas não tem como ser
      definido pela linguagem.
- [ ] **CLI só mostra o AST.** A mensagem "ainda não há interpretador"
      está desatualizada — o `run` deveria compilar e mostrar a timeline
      de eventos (e só reportar o primeiro erro hoje; mostrar todos).
- [ ] **Mensagens de erro misturam PT e EN** ("Erro léxico" vs.
      "Error in interpreter"). Escolher um idioma.

## Análise semântica

- [ ] Passo de validação entre parser e compilador: referência a
      `@nome` não declarado, `play` de algo que não é beat/melody/loop,
      tipo errado de parâmetro (`grid="abc"`), step fora do grid
      (`{ 17 }` com `grid=16`), nota com duração 0.
- [ ] Detectar ciclos (`loop @a { play @b; }` / `loop @b { play @a; }`).
- [ ] *Warnings* além de erros: declaração nunca tocada, variável não
      usada, parâmetro desconhecido.
- [ ] Erros de compilação com `Line`/`Column`, igual lexer/parser.

## Expressividade musical

- [ ] **Dinâmica:** `velocity`/`volume` por beat, por step
      (`{ 1:127, 5:80 }` ou `{ X.x. }` com maiúscula = acento) e por nota.
- [ ] **Swing / groove:** implementar `swing=0.2` no `play` e no `beat`.
- [ ] **Acordes:** `[C4 E4 G4] 0 1/2` ou nomes (`Cmaj7 0 1`).
- [ ] **Escalas e graus:** `melody @m (scale="C minor") { 1 0 1/4, 3 +0 1/4 }`.
- [ ] **Transposição:** `play @lead (transpose=+12)` / `(transpose=-2)`.
- [ ] **Pausas explícitas:** `_ 1/4` (ou `rest 1/4`) em vez de calcular
      o `+offset` na mão.
- [ ] **Ligaduras / legato e staccato** por nota.
- [ ] **Compasso no meio da música:** `@beatsPerBar` e `@bpm` por
      `loop`/trecho; mudança gradual (`accelerando`, `ritardando`).
- [ ] **Quiálteras / tercinas:** `grid=12` já ajuda, mas um açúcar como
      `1/8t` seria mais legível.
- [ ] **Probabilidade / humanização:** `chance=0.5` por step, `humanize=5ms`.
- [ ] **Amostras contínuas:** a "categoria C" do `exemplo-completo.spin`
      (um mp3 tocando como camada, com `offset`/`length`).

## Estrutura da linguagem

- [ ] **Modificadores de `play` além de `repeat`:** `offset`, `length`,
      `mute`, `reverse`, `every=2` (toca a cada N repetições), `fadeIn`/`fadeOut`.
- [ ] **Seções paralelas explícitas** dentro de `song` (hoje só `loop`
      empilha) — algo como `play @a & @b;`.
- [ ] **Parâmetros/argumentos em loops** (templates):
      `loop @groove(@kickSample) { ... }` reutilizável com samples diferentes.
- [ ] **Variações de pattern:** `beat @kick2 extends @kick { +16 }`
      (herdar e adicionar/remover steps).
- [ ] **Expressões simples:** `@bpm * 2`, `1/4 + 1/8`, concatenação de
      strings para URLs (`@base + "kick.wav"`).
- [ ] **`import "drums.spin";`** para separar kits/patterns em arquivos e
      ter uma "biblioteca padrão" de grooves.
- [ ] **Marcadores / seções nomeadas** no `song` para pular direto
      (`play @chorus from=bar 9`).
- [ ] **Escapes em strings** (`"\"`) e strings multilinha, se fizer falta.

## Áudio / mixagem

- [ ] **Canais / tracks:** `track @drums (volume=0.8, pan=-0.2) { ... }`.
- [ ] **Efeitos:** `reverb`, `delay`, `filter` (lowpass/highpass),
      `distortion` — mapeiam bem para nós do Web Audio.
- [ ] **Envelopes ADSR** por melody/instrumento.
- [ ] **Instrumentos multi-sample:** um sample por faixa de notas em vez
      de um só `rootNote` esticado (soa melhor nos extremos).
- [ ] **Síntese básica:** `sample=sine`/`saw`/`square` sem precisar de wav.
- [ ] **Exportar áudio:** CLI gerar `.wav` (render offline) e `.mid` (MIDI).

## Ferramentas

- [ ] **Language Server (LSP)** reaproveitando o `SpinScript.Core`:
      diagnósticos reais no VS Code (o parser já coleta vários erros),
      *go to definition* de `@nome`, autocomplete de referências e
      parâmetros, *hover* com a duração em ms.
- [ ] **Formatter** (`spinscript fmt`) — alinhar colunas de melody, por exemplo.
- [ ] **CLI:** `spinscript play` (tocar no terminal), `spinscript events`
      (dump JSON da timeline), `--watch` para recompilar ao salvar.
- [ ] **Playground:** mostrar erros inline no editor, piano roll /
      grid visual sincronizado com o código, cursor seguindo a reprodução,
      compartilhar música por URL.
- [ ] **Gramática do VS Code gerada/validada** contra o lexer, pra não
      divergir (hoje é mantida à mão).
- [ ] **Importar MIDI** → gerar `.spin`.

## Qualidade

- [ ] Testes de ponta a ponta: cada `.spin` de exemplo compila e gera
      uma timeline esperada (snapshot).
- [x] CI (GitHub Actions) rodando `dotnet test` e o build do WASM.
- [ ] Especificação formal da gramática (EBNF) no README.
- [ ] Exemplos novos cobrindo cada feature, num diretório `examples/`.
