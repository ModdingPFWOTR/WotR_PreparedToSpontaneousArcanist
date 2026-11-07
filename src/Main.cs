using HarmonyLib;
using UnityModManagerNet;
using allSpontaneous.UI;

namespace allSpontaneous
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony HarmonyInstance;
        public static Settings Settings;
        public static bool Enabled = true;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Settings = Settings.Load();
            HarmonyInstance = new Harmony(modEntry.Info.Id);

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            try
            {
                HarmonyInstance.PatchAll();
                Log("allSpontaneousArcanistStyle loaded.");
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
                Log("Mod enabled.");
            }
            else
            {
                Log("Mod disabled. Blueprint changes already applied this session stay until you revert them in the menu or restart the game.");
            }

            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UmmMenu.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings?.Save();
        }
    }
}
