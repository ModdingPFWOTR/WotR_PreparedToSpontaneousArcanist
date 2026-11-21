# allSpontaneousArcanistStyle

A [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) mod for *Pathfinder: Wrath of the Righteous*.

Prepared casters stay prepared. They still learn spells the way their class already does (a wizard still learns on level-up and from scrolls; a cleric still knows the full list) and they still fill spell slots after a rest. What changes is how those prepared spells are spent: every prepared spell of a given level draws from one shared cast pool for that level, the same way an Arcanist does.

The class does not become an Arcanist. It keeps its own spells-per-day table, its own casting ability (a wizard's Intelligence, a cleric's Wisdom, a paladin's Charisma), and extras such as Abundant Casting. Those bonuses raise the shared pool. The number of *different* spells you can prepare still follows the class table without the ability bonus and without Abundant Casting. Real Arcanist spellbooks are left alone. The change applies to the player and companions; enemies keep the original spellbook.

Each discovered class and archetype can be turned on or off from the mod menu. Choices are stored in `settings.json` in the mod folder.

## Install

1. Install Unity Mod Manager and apply it to Wrath of the Righteous.
2. Copy the `allSpontaneousArcanistStyle` folder into `<game>\Mods\`.
3. That folder needs `allSpontaneousArcanistStyle.dll` and `Info.json`.
4. Start the game and confirm the mod is listed and enabled in UMM.
5. In the UMM menu, tick the classes you want, or enable "Convert every prepared spellbook as it is discovered".

A typical Steam path:

`D:\SteamLibrary\steamapps\common\Pathfinder Second Adventure\Mods\allSpontaneousArcanistStyle`

On a character that already exists, rest once after turning a spellbook on so the slots refresh.

## Usage

- **Per spellbook.** Toggle a class in the list. Prepared spells of one circle share that circle's casts. Cantrips are unchanged.
- **Pool size.** The class table, plus that class's own ability modifier and extras such as Abundant Casting.
- **Prepared slots.** How many different spells fit in the day follows the class table, without the ability bonus and without Abundant Casting. Those bonuses increase casts, not the number of preparations.
- **Native Arcanists** are skipped.
- **Scan.** Blueprints load on demand. If the list is empty, open the main menu or a save, then click "Scan spellbooks".
- Restart the game after large changes on a character that is already created. Turning the mod off in UMM does not undo blueprint changes already applied this session; uncheck the class in the menu, or restart.

## BubbleBuffs

`BubbleBuffsCompat` is a separate mod for players who also use [BubbleBuffs](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/62). BubbleBuffs counts a prepared spell once per memorized copy unless the book is spontaneous or an Arcanist. This mod keeps those blueprint flags off so enemies stay on the original book, and the compat mod tells BubbleBuffs to use the shared pool instead. It loads after both BubbleBuffs and this mod. Reopen the spellbook after enabling or disabling it.

## Build

Requires the .NET SDK and a local install of the game (Unity Mod Manager already applied). `Directory.Build.props` points at the Steam folder; override `GamePath` there if yours is different.

```powershell
dotnet build
```

The build copies the DLL and `Info.json` into the game's `Mods` folder. `BubbleBuffsCompat` is a second project in the same solution.

## License

MIT. See [LICENSE](LICENSE).
