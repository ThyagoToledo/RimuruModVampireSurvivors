<p align="center">
  <img src="dist/custom-character/charsel.png" alt="Rimuru Tempest" width="144" />
</p>

<h1 align="center">Rimuru Mod for Vampire Survivors</h1>

<p align="center">
  Rimuru Tempest com formas Slime, Humanoid e Demon Lord, armas evolutivas, Ranga e habilidades inspiradas no anime.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=for-the-badge&logo=windows" alt="Windows 10 e 11" />
  <img src="https://img.shields.io/badge/Vampire%20Survivors-public--beta-B51F2E?style=for-the-badge" alt="Vampire Survivors public-beta" />
  <img src="https://img.shields.io/badge/BepInEx-6%20IL2CPP-5C2D91?style=for-the-badge" alt="BepInEx 6 IL2CPP" />
  <img src="https://img.shields.io/badge/Mod-0.4.2-2EA44F?style=for-the-badge" alt="Rimuru Mod 0.4.2" />
</p>

> [!IMPORTANT]
> Use a branch `public-beta` de Vampire Survivors. Feche o jogo antes de instalar ou reparar o mod.

---

## Instalação rápida

1. Baixe o repositório por `Code > Download ZIP` e extraia todos os arquivos.
2. Execute `Install-RimuruMod.bat`.
3. Escolha **Instalar ou reparar**.
4. Abra o jogo pela Steam e selecione Rimuru.

O mesmo arquivo também verifica, repara e remove o mod. Ele tenta localizar o jogo pelas bibliotecas Steam, pela pasta atual e pela pasta pai. Quando não encontra, permite colar a pasta do jogo ou arrastar `VampireSurvivors.exe` para a janela.

Também é possível colocar a pasta extraída dentro da pasta de Vampire Survivors e executar o `.bat` dali. Mantenha `Install-RimuruMod.bat` e a pasta `dist` juntos.

---

## O que o instalador faz

- Detecta automaticamente a instalação do jogo em bibliotecas Steam.
- Instala ou repara o personagem CUSTOM, o plugin e os assets.
- Baixa o BepInEx 6 IL2CPP x64 da fonte oficial somente quando necessário.
- Confere o SHA-256 do BepInEx e do plugin antes de instalar.
- Cria backup das versões anteriores.
- Compara todos os arquivos após a instalação.
- Preserva saves e nunca remove a instalação completa do BepInEx.

Na primeira abertura com BepInEx, o carregamento pode demorar mais enquanto o loader prepara os arquivos de interoperabilidade.

---

## Conteúdo

- **Slime:** Predador, regeneração, resistências e invocação de Ranga.
- **Humanoid:** katana de Rimuru, manipulação de água e Chama Negra.
- **Demon Lord:** Beelzebuth, Ciel, barreiras e armas evoluídas.
- **Azathoth:** terceira evolução planejada para enfrentar a Morte.

O projeto continua em beta. Progressão avançada, transformações e efeitos visuais ainda passam por testes de partida.

---

## Hub de documentação

- **[Instalação e remoção](doc/installation.md):** modos do instalador, caminhos e backups.
- **[Solução de problemas](doc/troubleshooting.md):** jogo não localizado, BepInEx, tela de carregamento e logs.
- **[Desenvolvimento](doc/development.md):** estrutura do código, compilação e atualização do pacote.

---

## Estrutura do projeto

```text
RimuruModVampireSurvivors/
|-- Install-RimuruMod.bat      # Instala, repara, verifica e remove
|-- dist/                      # Pacote pronto para jogadores
|   |-- custom-character/      # Personagem e skins CUSTOM
|   `-- plugin/                # Runtime BepInEx e assets próprios
|-- doc/                       # Guias técnicos
|-- source/                    # Código-fonte e testes
`-- README.md                  # Portal do projeto
```

---

## Desenvolvimento

O plugin deve ser compilado contra as assemblies IL2CPP geradas pela mesma build do jogo usada nos testes. Consulte o [guia de desenvolvimento](doc/development.md) antes de alterar o runtime.

---

## Autor

<table align="center">
  <tr>
    <td align="center">
      <a href="https://github.com/ThyagoToledo">
        <img src="https://github.com/ThyagoToledo.png?size=100" width="100" alt="Thyago Toledo" style="border-radius: 50%;" /><br />
        <sub><b>Thyago Toledo</b></sub>
      </a>
    </td>
  </tr>
</table>

---

## Aviso legal

Projeto de fã, gratuito, não comercial e não afiliado à poncle, Fuse ou aos detentores de *That Time I Got Reincarnated as a Slime*. O repositório não inclui arquivos do jogo, DLCs, saves, assemblies proprietárias ou configurações locais de autoria Git. BepInEx permanece sob sua própria licença.
