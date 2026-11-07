using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;

namespace allSpontaneous
{
    public static class SpellcasterScanner
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, SpellbookRecord> Records = new Dictionary<string, SpellbookRecord>(StringComparer.OrdinalIgnoreCase);
        private static bool _scanning;

        public static IReadOnlyList<SpellbookRecord> All
        {
            get
            {
                lock (Gate)
                {
                    return Records.Values
                        .OrderBy(r => r.OwnerLabel, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
        }

        public static void ScanFromRoot()
        {
            if (_scanning)
            {
                return;
            }

            _scanning = true;
            try
            {
                var root = ResourcesLibrary.GetRoot();
                var progression = root?.Progression;
                if (progression == null)
                {
                    Main.Log("Blueprint root/progression not ready.");
                    return;
                }

                SafeForEach(progression.CharacterClasses, ConsiderClass);
                SafeForEach(progression.PetClasses, ConsiderClass);
                SafeForEach(progression.CharacterMythics, ConsiderClass);
                ConsiderClass(progression.MythicStartingClass);
                ConsiderClass(progression.MythicCompanionClass);

                Main.Log($"Scan complete. Spellbooks: {Records.Count}");
            }
            catch (Exception ex)
            {
                Main.Log($"ScanFromRoot failed: {ex}");
            }
            finally
            {
                _scanning = false;
            }
        }

        public static void ScanLoaded()
        {
            ScanFromRoot();
        }

        private static void SafeForEach(IEnumerable<BlueprintCharacterClass> classes, Action<BlueprintCharacterClass> action)
        {
            if (classes == null)
            {
                return;
            }

            try
            {
                foreach (var characterClass in classes)
                {
                    action(characterClass);
                }
            }
            catch (Exception ex)
            {
                Main.Log($"Class list iteration failed: {ex}");
            }
        }

        private static void ConsiderClass(BlueprintCharacterClass characterClass)
        {
            if (characterClass == null)
            {
                return;
            }

            try
            {
                var className = SafeName(characterClass.Name, characterClass.name);
                Register(characterClass.Spellbook, className);

                IEnumerable<BlueprintArchetype> archetypes = null;
                try
                {
                    archetypes = characterClass.Archetypes;
                }
                catch (Exception ex)
                {
                    Main.Log($"Archetypes failed for {className}: {ex.Message}");
                }

                if (archetypes == null)
                {
                    return;
                }

                foreach (var archetype in archetypes)
                {
                    try
                    {
                        if (archetype == null || archetype.RemoveSpellbook || archetype.ReplaceSpellbook == null)
                        {
                            continue;
                        }

                        var archetypeName = SafeName(archetype.Name, archetype.name);
                        Register(archetype.ReplaceSpellbook, $"{className} [{archetypeName}]");
                    }
                    catch (Exception ex)
                    {
                        Main.Log($"Archetype failed for {className}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log($"Failed considering class {characterClass.name}: {ex}");
            }
        }

        private static string SafeName(string localized, string fallback)
        {
            if (!string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            return string.IsNullOrEmpty(fallback) ? "(unnamed)" : fallback;
        }

        private static void Register(BlueprintSpellbook spellbook, string owner)
        {
            if (spellbook == null)
            {
                return;
            }

            try
            {
                var guid = spellbook.AssetGuid.ToString();
                SpellbookRecord record;
                lock (Gate)
                {
                    if (!Records.TryGetValue(guid, out record))
                    {
                        record = new SpellbookRecord
                        {
                            Guid = guid,
                            SpellbookInternalName = string.IsNullOrEmpty(spellbook.name) ? guid : spellbook.name,
                            Spellbook = spellbook
                        };
                        Records[guid] = record;
                    }
                    else
                    {
                        record.Spellbook = spellbook;
                    }

                    if (!string.IsNullOrWhiteSpace(owner) && !record.Owners.Contains(owner))
                    {
                        record.Owners.Add(owner);
                        record.Owners.Sort(StringComparer.OrdinalIgnoreCase);
                    }
                }

                SpellbookConverter.ApplyFromSettings(record);
            }
            catch (Exception ex)
            {
                Main.Log($"Register failed for {spellbook.name}: {ex}");
            }
        }
    }
}
