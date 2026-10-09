# Reflexo Relâmpago · Fast-Drive

Segundo jogo do Fast-Drive Game. Alvos aparecem um de cada vez em lugares diferentes da tela, e a pessoa toca neles o mais rápido que puder, sem tocar nas distrações. A mecânica é a única parte deste projeto escrita para ele; telas, placar, tempo e pacotes vêm da Base e dos pacotes do Fast-Drive, que ficam no repositório [Tools](https://github.com/suricatus/Tools) (`Ferramentas/Tools/tools/FastDrive.*`).

O cliente padrão é a própria Suricatus: é a versão de demonstração para o site e para reuniões.

## Abrir e jogar

1. Clone este repositório e o Tools lado a lado, mantendo as pastas `GitHub/Suricatus/Jogos - Prateleira/` e `GitHub/Suricatus/Ferramentas/Tools/`: o jogo encontra os pacotes por caminho relativo.
2. Abra `reflexo-relampago` na Unity 6000.3.5f2.
3. Abra `Assets/Scenes/ReflexoRelampago.unity` e aperte Play.
4. No Game view, use 1080 × 1920 para o totem e o celular, ou 1920 × 1080 para o tablet, o computador e o site. O layout troca sozinho com a proporção.

Para recriar a cena do zero: **Fast-Drive > Montar cena do jogo**.

## Regras do jogo

- A partida dura 40 segundos (`tempos.partida` no `perfil.json`) e começa depois do "3, 2, 1, Já!".
- Os alvos aparecem um de cada vez na área quadrada. Tocar no alvo antes de ele sumir é um acerto; alvo que some sem toque é uma oportunidade perdida. O círculo atrás do alvo encolhe enquanto ele está na tela, mostrando quanto tempo falta.
- Três fases:
  - **Aquecimento** (primeiros 10 s): alvos maiores, mais tempo na tela, sem distrações.
  - **Desafio** (meio): alvos menores e distrações. O tempo na tela e o intervalo entre alvos vão diminuindo até os valores do sprint.
  - **Sprint final** (últimos 10 s): os alvos vêm com mais frequência, sem diminuir de tamanho.
- Às vezes, um alvo vem acompanhado de uma distração, que nunca encosta nele e some junto com ele. Tocar na distração tira pontos.
- A área de apoio mostra a fase, os acertos, a precisão e a legenda: o que tocar e o que evitar.

### Pontuação

| Jogada | Pontos |
|---|---|
| Acerto | 100 |
| Acerto rápido | Bônus de até 50: inteiro até 0,3 s depois de o alvo aparecer, caindo até zero em 1 s |
| Alvo perdido | 0 |
| Toque em distração | −50 (a pontuação nunca fica negativa) |

**Precisão** é acertos ÷ toques na área do jogo (no alvo, na distração ou no vazio): tocar a esmo derruba a precisão. Segundo toque rápido no mesmo alvo não conta.

### Resultado e faixas

O Resultado mostra a pontuação, a faixa ("Medalha de Ouro"), os acertos ("12 de 15", contando só os alvos que tiveram o tempo inteiro na tela) e a precisão.

| Faixa | Pontos | Na tela |
|---|---|---|
| Medalha de Bronze | 0 | "Quase lá!" |
| Medalha de Prata | 4000 | "Você venceu!", com confete e som de vitória |
| Medalha de Ouro | 5500 | "Você venceu!", com confete e som de vitória |

As faixas foram calibradas para a partida de 40 s (simulação de jogadores): quem joga na média faz perto de 4200 e chega à Prata; quem é rápido passa de 5500. **Mudou a duração ou o ritmo, ajuste as faixas e os brindes.** Brinde e retirada aparecem na tela Final, pelo pacote Prêmios.

## Pacotes

Um pacote é ligado em dois níveis:

1. **No jogo** (`reflexo-relampago/Packages/manifest.json`): o pacote está instalado e entra na build. Sem ele no manifesto, nenhum cliente deste jogo pode usá-lo.
2. **No cliente** (`perfil.json`, lista `pacotes`): o pacote funciona para aquele cliente. Instalado e fora da lista, ele não faz nada.

| Pacote | No jogo | Cliente Suricatus |
|---|---|---|
| `com.suricatusgames.fastdrive.base` | Sempre (é a Base, 0.9.0 ou mais nova) | — |
| `com.suricatusgames.fastdrive.efeitos` | Instalado | Ligado (`"efeitos"`): faíscas no acerto, moldura no erro, moldura pulsando no sprint final e papel picado na Prata e no Ouro |
| `com.suricatusgames.fastdrive.inatividade` | Instalado | Ligado (`"inatividade"`): volta para a Atração quando a pessoa sai |
| `com.suricatusgames.fastdrive.quiosque` | Instalado | Ligado (`"quiosque"`); no navegador fica parado |
| `com.suricatusgames.fastdrive.sons` | Instalado | Ligado (`"sons"`): efeitos padrão, sem música |
| `com.suricatusgames.fastdrive.teclado` | Instalado (usado pelo Cadastro) | — |
| `com.suricatusgames.fastdrive.cadastro` | Instalado | Ligado (`"cadastro"`): nome, e-mail e telefone |
| `com.suricatusgames.fastdrive.metricas` | Instalado | Ligado (`"metricas"`) |
| `com.suricatusgames.fastdrive.painel` | Instalado | Ligado (`"painel"`), senha de demonstração `2468`: 5 toques rápidos no canto de cima à esquerda da Atração |
| `com.suricatusgames.fastdrive.premios` | Instalado | Ligado (`"premios"`): adesivo e chaveiro a partir de 4000 pontos (Prata), caneca a partir de 5500 (Ouro) |
| `com.suricatusgames.fastdrive.ranking` | Instalado | Ligado (`"ranking"`): ranking do dia, 10 posições na Atração |
| `com.suricatusgames.fastdrive.servidor` | Instalado (usado pelo Cadastro, Prêmios e Métricas) | — |

Com o Modo quiosque ligado, a build de totem abre em tela cheia e não fecha com Alt+F4: para sair, **Ctrl+Shift+Q**.

Leads, brindes e métricas vão para o Supabase da Suricatus, o mesmo de todos os jogos, com `jogo: "reflexo-relampago"`. O endereço, a chave publishable e a chave da fila deste jogo ficam em `reflexo-relampago/Assets/FastDrive/Resources/FastDriveServidor.json`, iguais para todos os clientes do jogo (ver o README do pacote Servidor).

## Novo cliente

1. Copie `reflexo-relampago/Assets/Clientes/suricatus` para `reflexo-relampago/Assets/Clientes/<cliente-evento-ano>`.
2. Troque as imagens, as fontes, o `tema.json`, o `perfil.json`, as artes de `jogo/` e o `conteudo/reflexo.json`, mantendo os nomes de arquivo (ver o guia visual e as tabelas abaixo).
3. **Fast-Drive > Clientes > Atualizar clientes**.
4. Selecione o `cliente.asset` da pasta nova e use **Fast-Drive > Clientes > Usar cliente selecionado**. O Console mostra se falta algum arquivo ou se alguma cor tem contraste baixo.
5. Gere a build para a plataforma do evento.
6. Volte a cena para o cliente `suricatus`.

A pasta do cliente **nunca vai para o Git**: o `.gitignore` de `Assets/Clientes` só deixa subir a `suricatus`. Guarde as pastas de cliente fora do repositório.

### Artes do jogo (`jogo/`)

| Arquivo | O que é | Sem o arquivo |
|---|---|---|
| `alvo.png` ou `alvo-1.png`, `alvo-2.png`… | O que a pessoa deve tocar: logo, produto, mascote. Com várias, o jogo sorteia e nunca repete duas seguidas | O `marca/logo-quadrado.png` |
| `distracao.png` ou `distracao-1.png`, `distracao-2.png`… | O que a pessoa deve evitar | Um X branco num círculo na cor `erro` do tema |

PNG com fundo transparente, quadrado, 512 × 512 px, com a figura ocupando quase todo o quadrado. A distração precisa ser bem diferente do alvo à primeira vista (outra cor e outra forma; o vermelho com X funciona bem). No cliente Suricatus, os alvos são as 5 poses do suricato e a distração é o botão vermelho com X.

### `conteudo/reflexo.json`

Tudo é opcional; o que faltar fica com o padrão.

| Campo | O que é | Padrão |
|---|---|---|
| `fases.aquecimento.segundos` / `fases.sprint.segundos` | Duração do aquecimento e do sprint (0 a 30 s). O desafio fica com o resto da partida. Partida curta demais: as duas encolhem na mesma proporção | 10 / 10 |
| `fases.<fase>.tempoNaTela` | Quanto tempo cada alvo fica na tela (0,4 a 5 s) | 1,6 / 1,2 / 0,85 |
| `fases.<fase>.intervalo` | Espera entre um alvo sair e o próximo aparecer (0 a 3 s) | 0,5 / 0,35 / 0,15 |
| `fases.<fase>.tamanho` | Lado do alvo em fração da área do jogo (0,12 a 0,45) | 0,34 / 0,28 / 0,28 |
| `fases.<fase>.distracoes` | Chance de cada alvo vir com uma distração (0 a 1) | 0 / 0,3 / 0,3 |
| `pontosPorAcerto` | Pontos de cada acerto | 100 |
| `bonusRapido.pontos` | Bônus máximo por rapidez | 50 |
| `bonusRapido.inteiroAte` / `bonusRapido.zeraEm` | Até quantos segundos o bônus é inteiro / a partir de quantos ele zera | 0,3 / 1,0 |
| `penalidadeDistracao` | Pontos tirados no toque em distração | 50 |
| `faixas` | Lista de `{ "nome", "pontos", "vitoria" }`. O nome aparece no Resultado. Sem nenhum `vitoria`, todas menos a mais baixa contam como vitória | Bronze 0, Prata 4000, Ouro 5500 |
| `comoJogar` | Até 3 passos, com até 8 palavras cada | "Toque nos alvos assim que aparecerem", "Quanto mais rápido, mais pontos", "Evite as distrações: elas tiram pontos" |

Os valores de cada fase, na ordem aquecimento / desafio / sprint.

### Textos

Trocáveis em `textos` no `perfil.json`:

| Chave | Padrão |
|---|---|
| `reflexo.titulo` | Reflexo Relâmpago |
| `reflexo.subtitulo` | Toque nos alvos o mais rápido que puder! |
| `reflexo.fase.aquecimento`, `reflexo.fase.desafio`, `reflexo.fase.sprint` | Aquecimento, Desafio, Sprint final! |
| `reflexo.acertos`, `reflexo.precisao` | Acertos, Precisão |
| `reflexo.toque`, `reflexo.evite` | Toque, Evite |
| `reflexo.resultado` | Acertos: {acertos} de {alvos} · Precisão: {precisao}% |

## Testes

Window > General > Test Runner > EditMode: 28 testes do Reflexo Relâmpago (conteúdo e limites, pontuação e bônus, faixas, fases, partida: alvo perdido, acerto, distração, posições, precisão, fim), mais os testes dos pacotes instalados (26 da Base, 13 do Teclado, 30 do Servidor, 30 do Cadastro, 24 do Prêmios, 14 do Painel, 17 do Ranking, 19 do Sons, 11 do Efeitos, 13 do Métricas, 7 da Inatividade, 9 do Modo quiosque).

## Estrutura

```text
reflexo-relampago/
  Packages/manifest.json                   pacotes instalados no jogo
  Assets/ReflexoRelampago/                 só a mecânica do jogo
    Scripts/Runtime/Logic/                 conteúdo, fases e partida, sem Unity
    Scripts/Runtime/View/                  área do jogo, painel de apoio e desenho das peças (UI Toolkit)
    Resources/ReflexoRelampago.uss         estilos próprios do jogo
    Tests/EditMode/
  Assets/Clientes/<cliente>/               uma pasta por cliente, nomes fixos do guia visual
  Assets/FastDrive/                        PanelSettings, tema da interface e configuração do Servidor
  Assets/Scenes/ReflexoRelampago.unity
```
