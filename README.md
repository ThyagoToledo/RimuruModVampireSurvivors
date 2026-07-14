# Rimuru Mod for Vampire Survivors

Mod de fã, gratuito e não afiliado à poncle ou aos detentores de direitos de *That Time I Got Reincarnated as a Slime*.

## Instalação simples

1. Baixe este repositório em `Code > Download ZIP` ou clone-o.
2. Extraia o conteúdo para uma pasta local.
3. Execute `Install-RimuruMod.bat`.
4. Informe a pasta de instalação de Vampire Survivors se ela não for encontrada automaticamente.
5. Abra o jogo pelo Steam e escolha Rimuru.

O instalador:

- procura instalações Steam comuns e aceita um caminho manual;
- baixa o BepInEx 6 IL2CPP x64 oficial somente quando a cópia ainda não o possui;
- verifica o SHA-256 do arquivo baixado antes de usar;
- instala o personagem CUSTOM em `%LOCALAPPDATA%Low\poncle\Vampire Survivors\CustomCharacters\RIMURU`;
- instala o plugin e os assets em `BepInEx\plugins\RimuruSurvivor`;
- cria um backup em `%LOCALAPPDATA%\RimuruModVampireSurvivors\backups`;
- não inicia o jogo automaticamente e não altera saves.

Feche o jogo antes de instalar. Para uma primeira execução com BepInEx, o carregamento pode demorar mais enquanto o loader prepara as assemblies IL2CPP.

## Verificação e remoção

Execute `Verify-RimuruMod.bat` para conferir os arquivos instalados. Para remover apenas o mod e restaurar o último backup, execute:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\Uninstall-RimuruMod.ps1 -RestoreBackup
```

A remoção nunca apaga o BepInEx inteiro. Ela limita a limpeza às pastas exatas do personagem `RIMURU` e do plugin `RimuruSurvivor`.

## Conteúdo

- `dist/custom-character`: personagem, formas, sprites e loadouts CUSTOM.
- `dist/plugin`: plugin compilado e assets próprios de Rimuru, Beelzebuth, Predador, katana e Ranga.
- `source`: código, regras de progressão, testes e gerador de sprites para contribuidores.
- `installer`: instalação, verificação e remoção.

O estado atual é uma beta de runtime. Slime, Humanoid, Demon Lord, a renderização própria e Ranga estão no pacote; os ganchos adaptativos avançados permanecem desligados por compatibilidade com a build Unity validada. A evolução completa, Ciel e a luta contra a Morte continuam no roadmap.

## Build do plugin

O código depende das assemblies IL2CPP geradas pela sua própria cópia do jogo. Para compilar:

1. Instale o BepInEx IL2CPP na cópia de teste.
2. Execute o jogo uma vez e feche-o para gerar as assemblies.
3. Defina `BEPINEX_ROOT` para a pasta `BepInEx` dessa cópia.
4. Execute `dotnet build .\source\RimuruSurvivor.csproj -c Release`.

O binário pronto já está em `dist/plugin`; a compilação local é destinada a contribuidores e deve usar a mesma build do jogo.

## Fontes e licença

O instalador baixa o BepInEx diretamente dos [builds oficiais](https://builds.bepinex.dev/projects/bepinex_be), seguindo a [documentação de instalação IL2CPP](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html). BepInEx permanece sob sua própria licença.

Os assets originais deste mod são fornecidos para uso com este projeto. Não redistribua assets extraídos do jogo ou de DLCs. Este repositório não inclui perfil local de commit, saves, DLLs do jogo ou dados pessoais.
