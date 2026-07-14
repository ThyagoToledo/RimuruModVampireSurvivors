# Rimuru CUSTOM Character

Este pacote usa o formato oficial de personagens CUSTOM de Vampire Survivors.

## Instalacao para teste

1. No Steam, selecione a branch `public-beta` de Vampire Survivors.
2. Copie esta pasta para `%localappdata%low\poncle\Vampire Survivors\CustomCharacters\RIMURU`.
3. Inicie o jogo e escolha Rimuru na lista de personagens CUSTOM.

O formato CUSTOM adiciona personagem, estatisticas, arma inicial, localizacao, sprites e skins, mas nao cria um `WeaponType`. O template oficial tambem permite `startingWeapon`, `exWeapons`, `exAccessories` e `hiddenWeapons` por skin. Esta versao usa armas nativas validadas como equivalentes temporarios das habilidades do anime; a katana propria fica versionada em `../assets/katana/` para a arma definitiva do plugin.

## Habilidades por forma no CUSTOM

- Slime: `GARLIC` representa Predador, `SUMMONNIGHT` representa Ranga e os acessorios de regeneracao/magnetismo representam o corpo de slime.
- Humanoid: `NIGHTSWORD` representa a katana, `BUBBLES` representa manipulacao de agua e `FIREBALL` representa Chama Negra.
- Demon Lord: `NIGHTSWORD2` representa a katana evoluida; `SUMMONNIGHT2`, `SHADOWSERVANT2` e `LAUREL` representam as invocacoes, Beelzebuth e a barreira multicamada.

Esses equivalentes tornam as skins mecanicamente diferentes agora. A troca automatica Slime -> Humanoid -> Demon Lord e as armas proprias continuam sendo responsabilidade do plugin Harmony.

## Formato visual validado

- `charsel.png`: PNG RGBA de 48x48 pixels, usado somente na selecao.
- `sprites/rimuru_01.png` a `rimuru_04.png`: PNG RGBA de 32x32 pixels.
- Os nomes dos frames sao contiguos e usam o prefixo declarado em `spriteName`.
- O fundo e transparente e os pixels pretos internos sao preservados.
- A animacao alterna elevacao do corpo e posicao dos pes em quatro frames.
- `sprites/` e `charsel.png` representam Slime, a forma inicial.
- `skins/humanoid/` fornece a forma Humanoid, com quatro frames e retrato proprio.
- `skins/demon_lord/` fornece a forma Demon Lord, com quatro frames e retrato proprio.

## Limite da evolucao no CUSTOM

A skin Demon Lord fica desbloqueada para teste manual na selecao. O formato beta nao possui um gatilho de evolucao que troque a skin durante a fase, nem permite registrar a katana e seus projeteis como uma arma nova. A regra de evolucao, renascimento, Ciel e mira assistida esta no diretorio `source` deste repositorio e e aplicada pelo plugin BepInEx.

Para reconstruir os assets a partir da fonte, execute `tools/build_sprites.py` com Python e Pillow.

## Validacao

Testado na `public-beta` build Steam 24001654 (interface v1.15.110): selecao normal, fase iniciada, animacao carregada e a base `NIGHTSWORD` funcionando. Nao adicionar IDs a `exWeapons`, `exAccessories`, `hiddenWeapons` ou arcanas sem valida-los nesta build; um ID inexistente pode impedir o inicio da sessao.

## Arquivos

- `character.json`: estatisticas e loadouts nativos distintos para Slime, Humanoid e Demon Lord.
- `localization.json`: nomes e descricao em ingles e portugues.
- `charsel.png`: retrato RGBA 48x48 derivado da arte fornecida pelo usuario.
- `sprites/`: quatro frames RGBA 32x32 com caminhada alternada.
- `skins/demon_lord/`: retrato e frames da forma Demon Lord.
- `skins/humanoid/`: retrato e frames da forma Humanoid.
- `../plugin/assets/weapons/rimuru-katana-v2.png`: icone 32x32 da katana, com alfa.
- `../../source/rimuru-progression.json`: niveis, passiva, evolucao e contrato de troca de skin.
- `../../source/tools/build_sprites.py`: gerador deterministico dos assets.
