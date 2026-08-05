# Desenvolvimento

## Arquitetura

O mod possui duas camadas:

- `dist/custom-character`: integração oficial CUSTOM para personagem, seleção e skins.
- `dist/plugin`: runtime BepInEx e Harmony para progressão, transformações, Ranga, armas e resistências.

O código versionado fica em `source/src`. As regras independentes de Unity ficam em `source/src/RimuruRuntimeRules.cs` e possuem verificação executável em `source/tests`.

## Compilação

1. Instale BepInEx 6 IL2CPP x64 em uma cópia de teste da mesma build do jogo.
2. Execute o jogo uma vez e feche-o para gerar `BepInEx/interop`.
3. Defina `BEPINEX_ROOT` para a pasta `BepInEx` da cópia.
4. Compile o projeto.

```powershell
$env:BEPINEX_ROOT = 'C:\Games\Vampire Survivors Test\BepInEx'
dotnet build .\source\RimuruSurvivor.csproj -c Release
dotnet run --project .\source\tests\RimuruRuntimeRules.Check.csproj -c Release
```

## Atualização do pacote

1. Copie a DLL compilada para `dist/plugin/RimuruSurvivor.dll`.
2. Sincronize somente assets próprios usados pelo runtime.
3. Atualize `dist/manifest.json` com versão e SHA-256 da DLL.
4. Teste o instalador em uma cópia isolada usando caminhos temporários para CUSTOM e estado.
5. Execute instalação, verificação, reparo e remoção com restauração.
6. Faça uma partida de regressão antes de distribuir para a instalação Steam.

## Limites de distribuição

Não inclua arquivos do jogo, DLCs, saves, assemblies de interoperabilidade, configuração local de Git ou assets extraídos de terceiros. O instalador baixa BepInEx da fonte oficial e valida o pacote por hash.
