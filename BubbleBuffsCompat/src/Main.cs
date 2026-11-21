using HarmonyLib;
using UnityModManagerNet;

namespace allSpontaneous.Compat
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony HarmonyInstance;
        public static bool Enabled = true;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            modEntry.OnToggle = OnToggle;

            try
            {
                if (!PoolPatch.Apply(HarmonyInstance))
                {
                    Log("BubbleBuffs compat did not patch. Converted casters keep the prepared cast count inside BubbleBuffs.");
                }
            }
            catch (System.Exception ex)
            {
                Log($"Harmony patch failed: {ex}");
                return false;
            }

            return true;
        }

        public static void Log(string message)
        {
            ModEntry?.Logger.Log(message);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            if (value)
            {
                PoolPatch.Apply(HarmonyInstance);
                Log("Compat enabled. Reopen the spellbook so BubbleBuffs rebuilds cast counts.");
            }
            else
            {
                HarmonyInstance?.UnpatchAll(HarmonyInstance.Id);
                PoolPatch.MarkUnpatched();
                Log("Compat disabled. Reopen the spellbook. Blueprint flags were not changed.");
            }

            return true;
        }
    }
}
