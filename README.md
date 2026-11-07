# allSpontaneousArcanistStyle

Mod de Unity Mod Manager para Pathfinder: Wrath of the Righteous.

Converte spellbooks **prepared** para o estilo **Arcanist**: a classe continua com spellbook (e aprende por scroll, se já podia), prepara uma lista e gasta um pool spontaneous. O menu lista as classes/arquétipos descobertos em runtime; cada um pode ser ligado ou desligado. A configuração fica em `settings.json` na pasta do mod.

## Instalação

1. Instale o [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) e aplique no Wrath of the Righteous.
2. Copie a pasta `allSpontaneousArcanistStyle` para `<jogo>\Mods\`.
3. A pasta final deve conter `allSpontaneousArcanistStyle.dll` e `Info.json`.
4. Abra o jogo, confirme que o mod aparece no UMM e está habilitado.
5. No menu do UMM, marque as classes (ou use “Convert every prepared spellbook”).

Caminho típico Steam:

`D:\SteamLibrary\steamapps\common\Pathfinder Second Adventure\Mods\allSpontaneousArcanistStyle`

## Uso

- **Toggle por spellbook:** a classe continua com o livro dela (mago aprende no level-up e por scroll; clérigo continua conhecendo a lista). Você prepara magias em slots. As magias preparadas de um círculo gastam um pool compartilhado, como o Arcanista.
- **Quantidade:** o pool é o cálculo normal da classe. A tabela dela, mais o atributo de conjuração dela (Inteligência do mago, Sabedoria do clérigo, Carisma do paladino) e extras como Abundant Casting. Não troca para a tabela nem para o atributo de Arcanista ou Feiticeiro.
- **Slots preparados:** a quantidade de magias diferentes que cabem no dia segue a tabela da classe, sem o bônus de atributo e sem Abundant Casting. Esses bônus aumentam o pool de conjurações.
- **Arcanist** de verdade não é convertido.
- **Rescan:** se a lista estiver vazia, entre no menu principal ou num save e clique em rescan — os blueprints são carregados sob demanda.
- Reinicie o jogo se mudar muita coisa com um personagem já criado.

## Build (desenvolvimento)

```powershell
dotnet build
```

O DLL e o `Info.json` são copiados para a pasta Mods do jogo.
