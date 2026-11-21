using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;

namespace allSpontaneous.Compat
{
    /// <summary>
    /// BubbleBuffs counts a prepared spell once per memorized copy unless the blueprint is Arcanist or spontaneous.
    /// Converted party books keep those flags off, so enemies stay on the original blueprint. This patch only
    /// rewrites BubbleBuffs' own credit counters for books the main mod already treats as a shared pool.
    /// </summary>
    internal static class PoolPatch
    {
        private static bool _patched;
        private static bool _loggedLink;
        private static MethodInfo _appliesTo;
        private static FieldInfo _buffList;
        private static FieldInfo _casterQueue;
        private static FieldInfo _book;
        private static FieldInfo _spell;
        private static FieldInfo _credits;
        private static PropertyInfo _creditValue;

        public static void MarkUnpatched()
        {
            _patched = false;
        }

        public static bool Apply(Harmony harmony)
        {
            if (_patched)
            {
                return true;
            }

            Assembly bubbleBuffs = FindAssembly("BubbleBuffs");
            if (bubbleBuffs == null)
            {
                Main.Log("BubbleBuffs is not loaded.");
                return false;
            }

            Type stateType = bubbleBuffs.GetType("BubbleBuffs.BufferState");
            Type buffType = bubbleBuffs.GetType("BubbleBuffs.BubbleBuff");
            MethodInfo recalculate = stateType?.GetMethod("RecalculateAvailableBuffs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo creditsNeeded = buffType?.GetMethod("CreditsNeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (recalculate == null || creditsNeeded == null)
            {
                Main.Log("BubbleBuffs pool methods were not found. This compat build does not match the installed BubbleBuffs.");
                return false;
            }

            harmony.Patch(recalculate, postfix: new HarmonyMethod(typeof(PoolPatch), nameof(AfterRecalculate)));
            harmony.Patch(creditsNeeded, postfix: new HarmonyMethod(typeof(PoolPatch), nameof(AfterCreditsNeeded)));
            _patched = true;
            Main.Log("BubbleBuffs compat patched. Converted party spellbooks share one cast pool per spell level.");
            return true;
        }

        private static void AfterCreditsNeeded(AbilityData spell, ref int __result)
        {
            try
            {
                if (Main.Enabled && AppliesTo(spell?.Spellbook))
                {
                    __result = 1;
                }
            }
            catch (Exception ex)
            {
                Main.Log($"BubbleBuffs credit cost patch failed: {ex.Message}");
            }
        }

        private static void AfterRecalculate(object __instance)
        {
            if (!Main.Enabled || __instance == null)
            {
                return;
            }

            try
            {
                SharePools(__instance);
            }
            catch (Exception ex)
            {
                Main.Log($"BubbleBuffs pool patch failed: {ex.Message}");
            }
        }

        private static void SharePools(object state)
        {
            if (!ResolveFields(state))
            {
                return;
            }

            IEnumerable buffs = _buffList.GetValue(state) as IEnumerable;
            if (buffs == null)
            {
                return;
            }

            var groups = new Dictionary<PoolKey, List<object>>();
            foreach (object buff in buffs)
            {
                IEnumerable queue = _casterQueue.GetValue(buff) as IEnumerable;
                if (queue == null)
                {
                    continue;
                }

                foreach (object provider in queue)
                {
                    Spellbook book = _book.GetValue(provider) as Spellbook;
                    AbilityData spell = _spell.GetValue(provider) as AbilityData;
                    if (book == null || spell == null || !AppliesTo(book))
                    {
                        continue;
                    }

                    int level = book.GetSpellLevel(spell);
                    if (level < 1 || level > 10)
                    {
                        continue;
                    }

                    var key = new PoolKey(book, level);
                    if (!groups.TryGetValue(key, out List<object> providers))
                    {
                        providers = new List<object>();
                        groups.Add(key, providers);
                    }

                    providers.Add(provider);
                }
            }

            foreach (KeyValuePair<PoolKey, List<object>> group in groups)
            {
                object shared = _credits.GetValue(group.Value[0]);
                if (shared == null)
                {
                    continue;
                }

                if (_creditValue == null || _creditValue.DeclaringType != shared.GetType())
                {
                    _creditValue = shared.GetType().GetProperty("Value");
                }

                if (_creditValue == null)
                {
                    Main.Log("BubbleBuffs credit value property was not found.");
                    return;
                }

                int pool = Math.Max(0, group.Key.Book.GetSpontaneousSlots(group.Key.Level));
                _creditValue.SetValue(shared, pool, null);
                for (int i = 0; i < group.Value.Count; i++)
                {
                    _credits.SetValue(group.Value[i], shared);
                }
            }

            if (groups.Count > 0 && !_loggedLink)
            {
                _loggedLink = true;
                Main.Log($"BubbleBuffs compat: {groups.Count} converted spell level(s) now use the shared cast pool.");
            }
        }

        private static bool ResolveFields(object state)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _buffList = _buffList ?? state.GetType().GetField("BuffList", flags);
            if (_buffList == null)
            {
                Main.Log("BubbleBuffs BuffList field was not found.");
                return false;
            }

            if (_book != null && _spell != null && _credits != null && _casterQueue != null)
            {
                return true;
            }

            IEnumerable buffs = _buffList.GetValue(state) as IEnumerable;
            object sampleProvider = null;
            foreach (object buff in buffs ?? Array.Empty<object>())
            {
                _casterQueue = _casterQueue ?? buff.GetType().GetField("CasterQueue", flags);
                if (_casterQueue == null)
                {
                    continue;
                }

                sampleProvider = First(_casterQueue.GetValue(buff) as IEnumerable);
                if (sampleProvider != null)
                {
                    break;
                }
            }

            if (_casterQueue == null)
            {
                Main.Log("BubbleBuffs CasterQueue field was not found.");
                return false;
            }

            if (sampleProvider == null)
            {
                return false;
            }

            _book = _book ?? sampleProvider.GetType().GetField("book", flags);
            _spell = _spell ?? sampleProvider.GetType().GetField("spell", flags);
            _credits = _credits ?? sampleProvider.GetType().GetField("credits", flags);
            if (_book == null || _spell == null || _credits == null)
            {
                Main.Log("BubbleBuffs caster fields were not found.");
                return false;
            }

            return true;
        }

        private static bool AppliesTo(Spellbook spellbook)
        {
            if (spellbook == null)
            {
                return false;
            }

            if (_appliesTo == null)
            {
                Assembly mod = FindAssembly("allSpontaneousArcanistStyle");
                _appliesTo = mod?.GetType("allSpontaneous.SpellbookConverter")?.GetMethod(
                    "AppliesTo",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Spellbook) },
                    null);
                if (_appliesTo == null)
                {
                    return false;
                }
            }

            return (bool)_appliesTo.Invoke(null, new object[] { spellbook });
        }

        private static object First(IEnumerable items)
        {
            if (items == null)
            {
                return null;
            }

            foreach (object item in items)
            {
                return item;
            }

            return null;
        }

        private static Assembly FindAssembly(string name)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (string.Equals(assemblies[i].GetName().Name, name, StringComparison.Ordinal))
                {
                    return assemblies[i];
                }
            }

            return null;
        }

        private readonly struct PoolKey : IEquatable<PoolKey>
        {
            public PoolKey(Spellbook book, int level)
            {
                Book = book;
                Level = level;
            }

            public Spellbook Book { get; }
            public int Level { get; }

            public bool Equals(PoolKey other)
            {
                return ReferenceEquals(Book, other.Book) && Level == other.Level;
            }

            public override bool Equals(object obj)
            {
                return obj is PoolKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(Book) * 397) ^ Level;
                }
            }
        }
    }
}
