using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace allSpontaneous.Patches
{
    [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetAvailableForCastSpellCount))]
    internal static class Spellbook_GetAvailableForCastSpellCount_Patch
    {
        private static void Postfix(Spellbook __instance, AbilityData spell, ref int __result)
        {
            if (!SpellbookConverter.UsesSharedPool(__instance))
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
            if (spell == null || !SpellbookConverter.UsesSharedPool(__instance))
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
            if (spell == null || !SpellbookConverter.UsesSharedPool(__instance))
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
                if (book == null || !SpellbookConverter.UsesSharedPool(book))
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
}
