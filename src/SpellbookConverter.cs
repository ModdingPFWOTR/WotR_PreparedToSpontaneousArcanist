using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Parts;

namespace allSpontaneous
{
    public static class SpellbookConverter
    {
        private static readonly HashSet<string> ScaledSpellbooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static FieldInfo _spellsKnownField;
        private static FieldInfo _spontaneousSlotsField;
        private static FieldInfo _maxSpellLevelField;

        private static FieldInfo SpellsKnownField =>
            _spellsKnownField ?? (_spellsKnownField = AccessTools.Field(typeof(BlueprintSpellbook), "m_SpellsKnown"));

        private static FieldInfo SpontaneousSlotsField =>
            _spontaneousSlotsField ?? (_spontaneousSlotsField = AccessTools.Field(typeof(Spellbook), "m_SpontaneousSlots"));

        private static FieldInfo MaxSpellLevelField =>
            _maxSpellLevelField ?? (_maxSpellLevelField = AccessTools.Field(typeof(Spellbook), "m_MaxSpellLevel"));

        public static void ApplyFromSettings(SpellbookRecord record)
        {
            if (record?.Spellbook == null || Main.Settings == null || !Main.Enabled)
            {
                return;
            }

            try
            {
                var setting = Main.Settings.GetOrCreate(record.Guid);
                if (setting.OptOut || !setting.Converted)
                {
                    return;
                }

                Convert(record, setting);
            }
            catch (Exception ex)
            {
                Main.Log($"ApplyFromSettings failed for {record.SpellbookInternalName}: {ex}");
            }
        }

        public static void SetConverted(SpellbookRecord record, bool converted)
        {
            if (record?.Spellbook == null)
            {
                return;
            }

            try
            {
                var setting = Main.Settings.GetOrCreate(record.Guid);
                setting.Converted = converted;
                setting.OptOut = !converted;
                if (converted)
                {
                    Convert(record, setting);
                }
                else
                {
                    Revert(record, setting);
                }

                Main.Settings.Save();
            }
            catch (Exception ex)
            {
                Main.Log($"SetConverted failed for {record.SpellbookInternalName}: {ex}");
            }
        }

        public static void SetConvertAllPrepared(bool value)
        {
            try
            {
                Main.Settings.ConvertAllPrepared = value;
                foreach (var record in SpellcasterScanner.All)
                {
                    if (record.Spellbook == null || record.IsNativeArcanist)
                    {
                        continue;
                    }

                    var setting = Main.Settings.GetOrCreate(record.Guid);
                    if (setting.OptOut)
                    {
                        continue;
                    }

                    if (value && (!record.Spellbook.Spontaneous || setting.Converted))
                    {
                        setting.Converted = true;
                        Convert(record, setting);
                    }
                }

                Main.Settings.Save();
            }
            catch (Exception ex)
            {
                Main.Log($"SetConvertAllPrepared failed: {ex}");
            }
        }

        public static bool ShouldScale(BlueprintSpellbook spellbook)
        {
            if (!Main.Enabled || spellbook == null)
            {
                return false;
            }

            return ScaledSpellbooks.Contains(spellbook.AssetGuid.ToString());
        }

        public static bool UsesSharedPool(Spellbook spellbook)
        {
            return spellbook != null && ShouldScale(spellbook.Blueprint);
        }

        public static bool AppliesTo(Spellbook spellbook)
        {
            return UsesSharedPool(spellbook) && IsPlayerCaster(spellbook);
        }

        public static bool IsPlayerCaster(Spellbook spellbook)
        {
            try
            {
                UnitEntityData unit = spellbook?.Owner?.Unit;
                if (unit == null)
                {
                    return false;
                }

                if (unit.IsMainCharacter)
                {
                    return true;
                }

                UnitEntityData master = unit.IsPet ? unit.Master : unit;
                if (master == null)
                {
                    master = unit;
                }

                UnitPartCompanion companion = master.Get<UnitPartCompanion>();
                return companion != null && companion.State != CompanionState.None;
            }
            catch (Exception ex)
            {
                Main.Log($"IsPlayerCaster failed: {ex.Message}");
                return false;
            }
        }

        public static void SetSpontaneousSlots(Spellbook spellbook, int level, int count)
        {
            int[] slots = SpontaneousSlots(spellbook);
            if (slots == null || level < 0 || level >= slots.Length)
            {
                return;
            }

            slots[level] = count;
        }

        public static bool ExceedsMaxSpellLevel(Spellbook spellbook, int level)
        {
            object value = MaxSpellLevelField?.GetValue(spellbook);
            return value is int max && level >= max;
        }

        public static bool IsPreparedInSlot(Spellbook spellbook, AbilityData spell)
        {
            if (spellbook == null || spell?.Blueprint == null)
            {
                return false;
            }

            int level = spellbook.GetSpellLevel(spell);
            if (level < 1)
            {
                return true;
            }

            foreach (var slot in spellbook.GetMemorizedSpellSlots(level))
            {
                if (slot != null && slot.Available && slot.SpellShell?.Blueprint == spell.Blueprint)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPreparedInSlot(Spellbook spellbook, BlueprintAbility spell, int level, bool excludeSpecial)
        {
            if (spellbook == null || spell == null || level < 1)
            {
                return false;
            }

            foreach (var slot in spellbook.GetMemorizedSpellSlots(level))
            {
                if (slot == null || !slot.Available)
                {
                    continue;
                }

                if (excludeSpecial && slot.Type == SpellSlotType.Favorite)
                {
                    continue;
                }

                if (slot.SpellShell?.Blueprint == spell)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Convert(SpellbookRecord record, SpellbookSetting setting)
        {
            var spellbook = record.Spellbook;
            if (record.IsNativeArcanist)
            {
                return;
            }

            CaptureOriginals(spellbook, setting);
            KeepBlueprintVanilla(spellbook, setting);
            ScaledSpellbooks.Add(record.Guid);
            RefreshLoadedSpellbooks(spellbook);

            Main.Log($"Enabled arcanist-style casting for the party on {record.OwnerLabel} ({record.SpellbookInternalName}). Enemies keep the original spellbook.");
        }

        private static void RefreshLoadedSpellbooks(BlueprintSpellbook blueprint)
        {
            try
            {
                var characters = Kingmaker.Game.Instance?.Player?.AllCharacters;
                if (characters == null)
                {
                    return;
                }

                foreach (var unit in characters)
                {
                    if (unit?.Descriptor?.Spellbooks == null)
                    {
                        continue;
                    }

                    foreach (var book in unit.Descriptor.Spellbooks)
                    {
                        if (book?.Blueprint == blueprint)
                        {
                            book.UpdateAllSlotsSize(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log($"Refresh loaded spellbooks failed: {ex.Message}");
            }
        }

        private static void Revert(SpellbookRecord record, SpellbookSetting setting)
        {
            var spellbook = record.Spellbook;
            if (!setting.CapturedOriginals)
            {
                Main.Log($"Cannot revert {record.OwnerLabel}: original values were not captured.");
                return;
            }

            KeepBlueprintVanilla(spellbook, setting);
            ScaledSpellbooks.Remove(record.Guid);
            RefreshLoadedSpellbooks(spellbook);

            Main.Log($"Reverted {record.OwnerLabel} ({record.SpellbookInternalName}).");
        }

        private static void KeepBlueprintVanilla(BlueprintSpellbook spellbook, SpellbookSetting setting)
        {
            if (!setting.CapturedOriginals)
            {
                return;
            }

            spellbook.Spontaneous = setting.OriginalSpontaneous;
            spellbook.IsArcanist = setting.OriginalIsArcanist;
            spellbook.AllSpellsKnown = setting.OriginalAllSpellsKnown;
            RestoreSpellsKnown(spellbook, setting);
        }

        private static int[] SpontaneousSlots(Spellbook spellbook)
        {
            return SpontaneousSlotsField?.GetValue(spellbook) as int[];
        }

        private static void RestoreSpellsKnown(BlueprintSpellbook spellbook, SpellbookSetting setting)
        {
            if (string.IsNullOrEmpty(setting.OriginalSpellsKnownGuid))
            {
                SetSpellsKnown(spellbook, null);
                return;
            }

            var table = ResourcesLibrary.TryGetBlueprint<BlueprintSpellsTable>(setting.OriginalSpellsKnownGuid);
            SetSpellsKnown(spellbook, table != null ? table.ToReference<BlueprintSpellsTableReference>() : null);
        }

        private static void CaptureOriginals(BlueprintSpellbook spellbook, SpellbookSetting setting)
        {
            if (setting.CapturedOriginals)
            {
                return;
            }

            setting.OriginalSpontaneous = spellbook.Spontaneous;
            setting.OriginalIsArcanist = spellbook.IsArcanist;
            setting.OriginalAllSpellsKnown = spellbook.AllSpellsKnown;
            setting.OriginalSpellsKnownGuid = spellbook.SpellsKnown?.AssetGuid.ToString();
            setting.CapturedOriginals = true;
        }

        private static void SetSpellsKnown(BlueprintSpellbook spellbook, BlueprintSpellsTableReference value)
        {
            var field = SpellsKnownField;
            if (field == null)
            {
                Main.Log("Could not find m_SpellsKnown; skipped table assignment.");
                return;
            }

            field.SetValue(spellbook, value);
        }
    }
}
