# Reflexo Relâmpago · Fast-Drive

Segundo jogo do Fast-Drive Game, a arquitetura da Suricatus para fazer vários jogos com o mesmo layout, ligando e desligando pacotes por cliente. Leia o `README.md` deste repositório e o `README.md` da Base (`C:\Users\User\Documents\GitHub\Suricatus\Ferramentas\Tools\tools\FastDrive.Base`) antes de mudar código. O Caça Palavras (`Jogos - Prateleira\Suricatus - Caca Palavras`) é o jogo de referência.

## Regras de arquitetura

- **Base e pacotes ficam fora do jogo**, no repositório Tools (`Ferramentas/Tools/tools/FastDrive.*`, pacotes `com.suricatusgames.fastdrive.*`). O jogo os referencia no `Packages/manifest.json` por caminho relativo (`file:../../../../Ferramentas/Tools/tools/...`). Nunca crie pacote dentro de `reflexo-relampago/Packages` nem código de pacote em `Assets/`.
- **Base** (`FastDrive.Base`): fluxo, perfil, avisos, tema, layout e telas padrão. Nada específico de um jogo entra nela.
- **Jogo** (`Assets/ReflexoRelampago`): só a mecânica, via `GameplayProvider` e `MatchContext`. O jogo não monta telas da sequência nem mexe em placar e tempo. Erro que custa pontos é `match.Miss(penalidade)`.
- **Pacotes**: implementam `IFastDriveModule`, se registram no `ModuleRegistry` e só conversam por avisos (`SignalBus`). Nunca chamam o jogo nem outro pacote. Ligar um pacote tem dois níveis: instalado no `manifest.json` do jogo e listado em `pacotes` no `perfil.json` do cliente.
- **Clientes** (`Assets/Clientes/<cliente>`): só dados, com os nomes de arquivo fixos do guia visual. As artes do jogo (alvos e distrações) ficam em `jogo/`. Nenhuma imagem, fonte, texto ou número de um cliente fica no código: o código só tem os padrões, e o cliente troca pelo `perfil.json`, pelo `conteudo/reflexo.json` e pelos arquivos da pasta. Cliente não muda posição nem tamanho de nada; isso é projeto sob medida. Pastas de cliente nunca vão para o Git: só a `suricatus` sobe (`Assets/Clientes/.gitignore`), e a cena volta para ela antes de qualquer commit.
- O layout (posição e tamanho) fica em USS com as medidas do guia visual, em px na referência 1080 × 1920 (vertical) ou 1920 × 1080 (horizontal). Cor, fonte, cantos e texturas vêm do tema, aplicados pelo `UiFactory`.
- A sequência é fixa: Atração, Como jogar, Contagem, Gameplay, Resultado, Cadastro\*, Final\*. Não existe "Jogar de novo" (o documento de game design pede, mas a regra do Fast-Drive vale): o Final volta para a Atração.
- Plataformas: totem Windows, totem ou tablet Android, navegador (WebGL, site da Suricatus). Um aparelho por evento.

## Convenções

- Identificadores de código em inglês; textos de tela, mensagens de erro, comentários e documentação em português do Brasil.
- Textos de tela ficam em `Texts` (chaves `reflexo.*`, com reserva no código) ou no conteúdo do cliente, nunca soltos no código do jogo.
- Lógica do jogo sem dependência de Unity em `Scripts/Runtime/Logic`, com testes EditMode.
- As faixas padrão (Prata 4000, Ouro 5500) foram calibradas para a partida de 40 s. Mudou o ritmo ou a duração, refaça a conta e ajuste as faixas e os brindes.

## Trabalhando com o editor aberto

O projeto tem o pacote `com.unity.pipeline`: com a Unity aberta, use a CLI `unity` (skill unity-cli) para compilar (`recompile`, `console_status`), rodar testes (`run_tests --filter Suricatus.ReflexoRelampago.Tests --filter_type assembly`), montar a cena (`eval` com `Suricatus.FastDrive.Editor.GameSceneSetup.Setup("suricatus")`) e capturar a tela (`capture_game_view --width 540 --height 960 --save_path Temp/cap/x.png`; o arquivo vai para `Assets/Temp`, apague depois com `AssetDatabase.DeleteAsset("Assets/Temp")`).

Em segundo plano, a Unity só lê arquivos novos depois de `AssetDatabase.Refresh()` (por `eval`) ou quando ganha foco. Com `set_autotick` ligado, o Play mode anda em tempo real mesmo sem foco: a partida corre enquanto você olha as capturas. Para revisar telas, avance o fluxo chamando `GameHost.Next` com a tela atual (campo privado `current`), tudo no mesmo `eval`. Para a horizontal, `UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "...")` (e volte para 1080 × 1920 depois).

Um erro de `com.unity.collections` sobre `System.Runtime.CompilerServices.Unsafe.dll` no Console vem do cache de pacotes da Unity e não tem relação com o projeto.
