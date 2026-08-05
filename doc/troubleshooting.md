# Solução de problemas

## O jogo não foi encontrado

Confirme que o caminho escolhido contém `VampireSurvivors.exe`. Você pode arrastar o executável para a janela do instalador. Aspas adicionadas pelo Windows são removidas automaticamente.

## Pacote extraído incompleto

Não execute o `.bat` diretamente de dentro do ZIP. Extraia todo o repositório e mantenha `Install-RimuruMod.bat` ao lado da pasta `dist`.

## O jogo está aberto

Feche Vampire Survivors e aguarde alguns segundos. O instalador só bloqueia a instância pertencente ao caminho selecionado e não encerra o processo automaticamente.

## BepInEx não foi instalado

O instalador precisa de acesso HTTPS a `builds.bepinex.dev`. O download possui SHA-256 fixado; qualquer arquivo diferente é recusado. Antivírus ou proxy corporativo podem bloquear `winhttp.dll` ou o download.

## Primeira inicialização lenta

É esperado que a primeira execução com BepInEx seja mais demorada. O loader gera arquivos em `BepInEx\interop` para a build atual do jogo.

## Rimuru não aparece

1. Confirme que está usando a branch `public-beta` compatível.
2. Execute o instalador e escolha **Verificar instalação**.
3. Confirme a existência de `CustomCharacters\RIMURU\character.json` no caminho mostrado.

## Versão incompatível

Confira se o jogo está na branch `public-beta` e na build Unity `6000.0.62f1`. O mod
não promete compatibilidade com a branch estável ou outra build sem recompilação e QA.

## Plugin não carrega

Consulte `<jogo>\BepInEx\LogOutput.log` e procure por `Rimuru Tempest`. O início esperado contém a versão do plugin, o smoke test do Harmony e `Runtime jogavel iniciado`.

Se a build do jogo mudou, não reutilize assemblies IL2CPP antigas para recompilar o plugin. Gere novamente com a mesma versão do jogo.

## Reparo automático

Escolha **Verificar instalação**. Quando houver arquivos ausentes ou diferentes, o instalador oferece reinstalação automática com um novo backup.
