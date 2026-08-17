# SpinScript

Linguagem para criar música por código. Você descreve a batida, os
padrões e a estrutura da faixa em texto, e o SpinScript gera os eventos
de som. A inspiração é o OpenSCAD: assim como lá você programa um objeto
3D em vez de desenhar com o mouse, aqui você programa a música em vez de
clicar numa timeline.

Arquivos usam a extensão `.spin`.

sudo dotnet workload install wasm-tools

---

## Os conceitos, do menor pro maior

A linguagem tem três níveis. Entender a hierarquia é entender a
linguagem inteira.

**beat** é o que soa e onde soa. É a fileira de quadradinhos do FL
Studio: uma sequência de posições onde um som dispara. Um beat conhece
o próprio som (um kick, um chimbal) e a própria resolução.

**loop** empilha beats e define quanto tempo dura. Vários beats
dentro do mesmo loop tocam ao mesmo tempo (kick, caixa e chimbal juntos
formam um groove). O loop é medido em compassos.

**song** sequencia loops no tempo. É o roteiro da faixa: toca o refrão,
depois duas estrofes, depois o refrão de novo. A estrutura macro da
música vive aqui.

Resumindo o fluxo: beats são as camadas, loops as agrupam em trechos,
o song monta a faixa com esses trechos.

---

## Grid e bar: a distinção que mais confunde

São duas medidas diferentes e perpendiculares.

**grid** é a resolução de um beat: em quantos passos (steps) o
compasso é fatiado. `grid=16` significa dezesseis quadradinhos onde você
pode colocar um som. É exatamente a fileira de steps do FL Studio. Mexer
no grid muda a finura das batidas, não a duração.

**bars** é a duração de um loop, contada em compassos. Um compasso (bar)
é um grupo de quatro batidas, aquele "1, 2, 3, 4" que se conta antes da
música. `bars=4` significa que o loop dura quatro compassos. Mexer nos
bars muda o comprimento, não a finura.

A relação entre eles, no caso mais comum:

```
1 bar = 4 batidas = 16 steps (quando grid=16)
```

Então cada step de um `grid=16` dura um quarto de batida. Com o BPM, isso
vira tempo real: a 120 BPM cada batida dura 0,5s, logo cada step dura
0,125s. Regra de três direta a partir do BPM.

Quando um beat é mais curto que o loop que o contém, ele se repete
para preencher. Um beat de 1 bar dentro de um loop de `bars=4` toca
quatro vezes seguidas. É o comportamento de qualquer drum machine: o
groove de um compasso loopa para encher o trecho.

---

## Variáveis

Configuração global no topo do arquivo. Começam com `@` e terminam com
ponto e vírgula.

```
@bpm = 75;
```

O `@` marca toda referência nomeada na linguagem, tanto na definição
quanto no uso. Quando você vê `@` na frente de um nome, sabe na hora que
aquilo é algo definido em algum lugar do arquivo.

Nomes de variável começam com letra e podem conter letras, números e
underscore: `@bpm`, `@main_groove`, `@drop2` são válidos; `@2fast` e
`@$x` não.

---

## beats

Um beat declara um som e as posições onde ele dispara. Os parâmetros
vão entre parênteses; o conteúdo, entre chaves.

Percussão usa a grade de steps. As posições são só os números dos steps
onde o som toca:

```
beat @kick (sound=kick, grid=16) { 9 }
beat @hats (sound=hihat, grid=16) { 3, 7, 11, 15 }
```

O `kick` acima dispara só no step 9. O `hats` dispara nos steps 3, 7, 11
e 15. (Esse é, por sinal, o desenho de um groove de reggae: o kick no
tempo 3 do compasso, o chimbal nas contratempos.)

Instrumentos melódicos usam tempo livre em vez da grade, declarado com
`free`. Aqui cada evento carrega a nota, a duração e o momento em que
começa, medidos em batidas. (Essa parte da sintaxe ainda vai ser
refinada quando entrarmos em notas e acordes de verdade.)

```
beat @baixo (sound=bass, free) {
  // nota  duracao  inicio(em batidas)
  C2       1        0
  G2       1        2
}
```

A ideia é que percussão vive na grade (liga/desliga em steps) e melodia
vive no tempo contínuo (nota em qualquer ponto, inclusive entre dois
steps).

---

## Loops

Um loop agrupa beats e define a duração em bars. Todo beat tocado
dentro dele soa simultaneamente.

```
loop @groove (bars=1) {
  play @kick;
  play @snare;
  play @hats;
}
```

Os três beats tocam empilhados: é assim que se montam camadas. Não
existe sintaxe especial para "tocar junto"; basta pôr no mesmo loop.

O `play` aceita modificadores entre parênteses, no mesmo estilo dos
parâmetros de definição:

```
play @groove (repeat=2, swing=0.2);
```

`repeat` toca o alvo mais de uma vez; `swing` atrasa as subdivisões
pares para dar aquele balanço que tira o som de cima da grade rígida.
Novos modificadores entram nessa mesma lista sem mudar a gramática.

---

## Song

O `song` é a linha do tempo da faixa. Cada `play` toca um loop, e a
ordem dos `play` é a ordem da música.

```
song {
  play @refrao;
  play @estrofe (repeat=2);
  play @refrao;
  play @outro;
}
```

Lido de cima para baixo, isso é o roteiro da música: refrão, duas
estrofes, refrão, encerramento. A estrutura macro fica legível quase como
prosa.

---

## Regra de símbolos

Uma convenção única em toda a linguagem, sem exceção:

- **parênteses `( )`** cercam parâmetros e modificadores, tanto na
  definição (`loop x (bars=1)`) quanto no uso (`play @x (repeat=2)`).
- **chaves `{ }`** cercam corpo: o conteúdo de um beat, de um loop, de
  um song.
- **ponto e vírgula `;`** encerra cada instrução simples (variável,
  play).

Parênteses são sempre configuração; chaves são sempre conteúdo. Manter
esses dois papéis separados evita ambiguidade na leitura e no parser.

---

## Comentários

Ignorados pelo interpretador; servem só para quem lê o código.

```
// comentário de uma linha

/* comentário
   de bloco,
   várias linhas */
```

---

## Exemplo completo (só percussão)

Uma faixa de reggae reduzida à bateria, para mostrar as peças
trabalhando juntas. Andamento lento, batida one-drop, chimbal nas
contratempos.

```
@bpm = 75;

beat @kick  (sound=kick,  grid=16) { 9 }
beat @snare (sound=snare, grid=16) { 9 }
beat @hats  (sound=hihat, grid=16) { 3, 7, 11, 15 }

loop groove (bars=1) {
  play @kick;
  play @snare;
  play @hats;
}

loop refrao (bars=4) {
  play @groove;
}

loop estrofe (bars=4) {
  play @groove;
}

loop outro (bars=2) {
  play @hats;
}

song {
  play @refrao;
  play @estrofe (repeat=2);
  play @refrao;
  play @estrofe;
  play @refrao;
  play @estrofe;
  play @refrao;
  play @outro;
}
```

Note dois pontos que esse exemplo assume e que valem confirmar como
regra:

1. Um loop pode dar `play` em **outro loop** (aqui `refrao` toca
   `@groove`), não só em beats. Isso é o que elimina a repetição de
   escrever kick/snare/hats em cada trecho.
2. O beat de 1 bar dentro do loop de 4 bars se **repete** para
   preencher os quatro compassos.

---

## Vocabulário rápido

- **step**: um quadradinho da grade; a menor posição onde um som dispara.
- **grid**: quantos steps cabem no compasso de um beat (a resolução).
- **bar / compasso**: quatro batidas; a unidade de duração de um loop.
- **bpm**: batidas por minuto; define quanto tempo real dura cada batida.
- **beat**: um som e suas posições.
- **loop**: beats empilhados, com duração em bars.
- **song**: a sequência de loops que forma a faixa.
- **intro / outro**: o trecho de abertura e o de encerramento da música.