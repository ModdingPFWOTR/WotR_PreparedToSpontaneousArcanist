using System.Collections;
using HarmonyLib;
using Kingmaker;

namespace allSpontaneous.Patches
{
    [HarmonyPatch(typeof(MainMenu), "Start")]
    internal static class MainMenu_Start_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(MainMenu __instance)
        {
            __instance.StartCoroutine(ScanNextFrame());
        }

        private static IEnumerator ScanNextFrame()
        {
            yield return null;
            SpellcasterScanner.ScanFromRoot();
        }
    }
}
