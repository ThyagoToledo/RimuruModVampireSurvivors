# Instalação e remoção

## Requisitos

- Windows 10 ou 11.
- Vampire Survivors na branch `public-beta` compatível.
- Build validada: Unity `6000.0.62f1` (Vampire Survivors public-beta).
- BepInEx esperado: `6.0.0-be.785` IL2CPP x64.
- Pacote extraído por completo, com `Install-RimuruMod.bat` e `dist` lado a lado.
- Jogo fechado durante instalação, reparo, verificação ou remoção.

## Menu do instalador

Execute `Install-RimuruMod.bat` com duplo clique.

1. **Instalar ou reparar:** detecta o jogo, cria backup, instala BepInEx quando necessário, copia o mod e valida os arquivos.
2. **Verificar instalação:** compara o pacote com os arquivos instalados e oferece reparo automático.
3. **Remover mod:** remove apenas `RIMURU` e `RimuruSurvivor`; pode restaurar o backup anterior.
4. **Sair:** fecha sem alterar arquivos.

## Detecção do jogo

O instalador verifica, nesta ordem:

1. Caminho informado por parâmetro.
2. Pasta onde o `.bat` está localizado.
3. Pasta pai do pacote.
4. Pasta atual do terminal.
5. Última instalação registrada.
6. Registro do Steam e todas as bibliotecas declaradas em `libraryfolders.vdf`.
7. Pastas Steam comuns nos discos disponíveis.

Se nenhuma opção for válida, cole a pasta que contém `VampireSurvivors.exe` ou arraste o executável para o prompt.

## Uso por terminal

```powershell
./Install-RimuruMod.bat -Mode Install -GamePath "E:\SteamLibrary\steamapps\common\Vampire Survivors"
./Install-RimuruMod.bat -Mode Verify -GamePath "E:\SteamLibrary\steamapps\common\Vampire Survivors"
./Install-RimuruMod.bat -Mode Uninstall -GamePath "E:\SteamLibrary\steamapps\common\Vampire Survivors"
```

Para automação sem perguntas:

```powershell
./Install-RimuruMod.bat -Mode Install -GamePath "C:\Games\Vampire Survivors" -NonInteractive
```

## Destinos

- Personagem CUSTOM: `%USERPROFILE%\AppData\LocalLow\poncle\Vampire Survivors\CustomCharacters\RIMURU`
- Plugin: `<jogo>\BepInEx\plugins\RimuruSurvivor`
- Estado e backups: `%LOCALAPPDATA%\RimuruModVampireSurvivors`

O instalador não altera saves. A remoção preserva BepInEx para evitar apagar plugins de terceiros.

## Distribuição

O pacote não inclui arquivos proprietários do jogo nem assemblies geradas pelo IL2CPP.
O jogador baixa o ZIP, extrai tudo e executa `Install-RimuruMod.bat`; se a instalação
não for detectada, informa a pasta que contém `VampireSurvivors.exe`.
