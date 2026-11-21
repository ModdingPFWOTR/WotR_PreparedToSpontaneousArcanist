using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace allSpontaneous.Patches
{
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetAvailableForCastSpellCount))]
    internal static class Spellbook_GetAvailableForCastSpellCount_Patch
    {
        private static void Postfix(Spellbook __instance, AbilityData spell, ref int __result)
        {
            if (!SpellbookConverter.AppliesTo(__instance))
            {
                return;
            }

            int level = __instance.GetSpellLevel(spell);
            if (level < 1)
            {
                return;
            }

            if (!SpellbookConverter.IsPreparedInSlot(__instance, spell))
            {
                __result = 0;
                return;
            }

            __result = __instance.GetSpontaneousSlots(level);
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.CanSpend), typeof(AbilityData), typeof(bool))]
    internal static class Spellbook_CanSpend_Patch
    {
        private static bool Prefix(Spellbook __instance, AbilityData spell, ref bool __result)
        {
            if (spell == null || !SpellbookConverter.AppliesTo(__instance))
            {
                return true;
            }

            if (__instance.GetSpellLevel(spell) < 1 || SpellbookConverter.IsPreparedInSlot(__instance, spell))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.Spend), typeof(AbilityData), typeof(bool))]
    internal static class Spellbook_Spend_Patch
    {
        private static bool Prefix(Spellbook __instance, AbilityData spell, ref bool __result)
        {
            if (spell == null || !SpellbookConverter.AppliesTo(__instance))
            {
                return true;
            }

            if (__instance.GetSpellLevel(spell) < 1 || SpellbookConverter.IsPreparedInSlot(__instance, spell))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(ActionBarSpellbookHelper), nameof(ActionBarSpellbookHelper.Fetch))]
    internal static class ActionBarSpellbookHelper_Fetch_Patch
    {
        private static void Postfix(ref List<MechanicActionBarSlotSpell> __result)
        {
            if (__result == null || __result.Count == 0)
            {
                return;
            }

            __result = __result.Where(slot =>
            {
                var spell = slot?.Spell;
                var book = spell?.Spellbook;
                if (book == null || !SpellbookConverter.AppliesTo(book))
                {
                    return true;
                }

                if (book.GetSpellLevel(spell) < 1)
                {
                    return true;
                }

                return SpellbookConverter.IsPreparedInSlot(book, spell);
            }).ToList();
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.PostLoad))]
    internal static class Spellbook_PostLoad_Patch
    {
        private static void Postfix(Spellbook __instance)
        {
            if (!SpellbookConverter.UsesSharedPool(__instance))
            {
                return;
            }

            __instance.UpdateAllSlotsSize(true);
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellSlotsCount))]
    internal static class Spellbook_GetSpellSlotsCount_Patch
    {
        private static void Postfix(Spellbook __instance, int spellLevel, ref int __result)
        {
            if (!SpellbookConverter.AppliesTo(__instance))
            {
                return;
            }

            BlueprintSpellsTable table = __instance.Blueprint?.SpellSlots;
            if (table == null)
            {
                return;
            }

            __result = Math.Max(0, table.GetCount(__instance.CasterLevel, spellLevel));
        }
    }

    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.Rest))]
    internal static class Spellbook_Rest_Patch
    {
        private static void Postfix(Spellbook __instance)
        {
            if (!SpellbookConverter.AppliesTo(__instance))
            {
                return;
            }

            for (int level = 0; level <= 10; level++)
            {
                SpellbookConverter.SetSpontaneousSlots(__instance, level, __instance.GetSpellsPerDay(level));
            }
        }
    }

    [HarmonyPatch(typeof(Spellbook), "SpendInternal")]
    internal static class Spellbook_SpendInternal_Patch
    {
        private static bool Prefix(Spellbook __instance, BlueprintAbility blueprint, AbilityData spell, bool doSpend, bool excludeSpecial, ref bool __result)
        {
            if (!SpellbookConverter.AppliesTo(__instance))
            {
                return true;
            }

            if (doSpend && spell == null)
            {
                __result = false;
                return false;
            }

            int level = spell != null ? __instance.GetSpellLevel(spell) : __instance.GetMinSpellLevel(blueprint);
            if (level < 0)
            {
                __result = false;
                return false;
            }

            if (SpellbookConverter.ExceedsMaxSpellLevel(__instance, level))
            {
                __result = false;
                return false;
            }

            if (!__instance.IsStandaloneMythic && !HasCastingStat(__instance, level))
            {
                __result = false;
                return false;
            }

            if (level < 1)
            {
                __result = true;
                return false;
            }

            bool prepared = spell != null
                ? SpellbookConverter.IsPreparedInSlot(__instance, spell)
                : SpellbookConverter.IsPreparedInSlot(__instance, blueprint, level, excludeSpecial);
            if (!prepared)
            {
                __result = false;
                return false;
            }

            int pool = __instance.GetSpontaneousSlots(level);
            if (pool <= 0)
            {
                __result = false;
                return false;
            }

            if (doSpend)
            {
                SpellbookConverter.SetSpontaneousSlots(__instance, level, pool - 1);
            }

            __result = true;
            return false;
        }

        private static bool HasCastingStat(Spellbook spellbook, int level)
        {
            var stat = spellbook.Owner?.Stats?.GetStat(spellbook.Blueprint.CastingAttribute);
            return stat != null && (int)stat >= 10 + level;
        }
    }

    [HarmonyPatch(typeof(AbilityData), "get_IsSpontaneous")]
    internal static class AbilityData_IsSpontaneous_Patch
    {
        private static void Postfix(AbilityData __instance, ref bool __result)
        {
            if (__result || !SpellbookConverter.AppliesTo(__instance?.Spellbook))
            {
                return;
            }

            __result = true;
        }
    }
}
