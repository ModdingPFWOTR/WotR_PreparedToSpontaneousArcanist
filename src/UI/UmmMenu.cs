using System;
using UnityEngine;
using UnityModManagerNet;

namespace allSpontaneous.UI
{
    internal static class UmmMenu
    {
        private static Vector2 _scroll;
        private static string _filter = string.Empty;
        private static bool _showSpontaneous;

        public static void Draw(UnityModManager.ModEntry modEntry)
        {
            if (Main.Settings == null)
            {
                GUILayout.Label("Settings not loaded.");
                return;
            }

            GUILayout.Label("Prepared spellbooks keep their own slots and casting stat. Prepared spells of a level share that level's cast pool, like an arcanist. The pool is the class table plus that class's own ability bonus and extras such as Abundant Casting. Rest once after enabling this on an existing character.");
            GUILayout.Space(6);

            var convertAll = GUILayout.Toggle(Main.Settings.ConvertAllPrepared, "Convert every prepared spellbook as it is discovered");
            if (convertAll != Main.Settings.ConvertAllPrepared)
            {
                SpellbookConverter.SetConvertAllPrepared(convertAll);
            }

            _showSpontaneous = GUILayout.Toggle(_showSpontaneous, "Show spellbooks that are already spontaneous");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter", GUILayout.Width(50));
            _filter = GUILayout.TextField(_filter ?? string.Empty, GUILayout.Width(260));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Scan spellbooks", GUILayout.Width(220)))
            {
                SpellcasterScanner.ScanFromRoot();
            }

            var records = SpellcasterScanner.All;
            GUILayout.Label($"Discovered spellbooks: {records.Count}");
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(420));
            var shown = 0;
            foreach (var record in records)
            {
                if (record.Spellbook == null)
                {
                    continue;
                }

                if (!_showSpontaneous && record.Spellbook.Spontaneous && !record.IsConvertedByUs)
                {
                    continue;
                }

                if (!MatchesFilter(record))
                {
                    continue;
                }

                shown++;
                DrawRow(record);
            }

            if (shown == 0)
            {
                GUILayout.Label("No spellbooks match this filter yet. Open the main menu or a save so class blueprints load, then rescan.");
            }

            GUILayout.EndScrollView();
        }

        private static void DrawRow(SpellbookRecord record)
        {
            var book = record.Spellbook;
            var converted = record.IsConvertedByUs;
            var status = record.IsNativeArcanist
                ? "arcanist (skipped)"
                : converted
                    ? "arcanist-style"
                    : book.Spontaneous
                        ? "spontaneous"
                        : "prepared";

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUI.enabled = !record.IsNativeArcanist;
            var isOn = converted || (Main.Settings.ConvertAllPrepared && !record.IsNativeArcanist && !Main.Settings.GetOrCreate(record.Guid).OptOut && record.IsPrepared);
            var next = GUILayout.Toggle(isOn, "", GUILayout.Width(20));
            GUI.enabled = true;

            GUILayout.Label($"{record.OwnerLabel}", GUILayout.Width(360));
            GUILayout.Label(status, GUILayout.Width(170));
            GUILayout.EndHorizontal();

            GUILayout.Label($"  book: {record.SpellbookInternalName}  |  {record.Guid}");
            GUILayout.Label($"  Spells per day: {TableName(book.SpellsPerDay)}  |  Spells known: {TableName(book.SpellsKnown)}  |  All known: {book.AllSpellsKnown}");

            if (!record.IsNativeArcanist && next != isOn)
            {
                SpellbookConverter.SetConverted(record, next);
            }

            GUILayout.EndVertical();
        }

        private static bool MatchesFilter(SpellbookRecord record)
        {
            if (string.IsNullOrWhiteSpace(_filter))
            {
                return true;
            }

            return (record.OwnerLabel?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (record.SpellbookInternalName?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (record.Guid?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private static string TableName(Kingmaker.Blueprints.Classes.Spells.BlueprintSpellsTable table)
        {
            if (table == null)
            {
                return "(none)";
            }

            return string.IsNullOrEmpty(table.name) ? table.AssetGuid.ToString() : table.name;
        }
    }
}
