using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace allSpontaneous
{
    [Serializable]
    public sealed class SpellbookSetting
    {
        public bool Converted;
        public bool OptOut;
        public bool OriginalSpontaneous;
        public bool OriginalIsArcanist;
        public bool OriginalAllSpellsKnown;
        public string OriginalSpellsKnownGuid;
        public bool CapturedOriginals;
    }

    [Serializable]
    public sealed class Settings
    {
        public bool ConvertAllPrepared;
        public Dictionary<string, SpellbookSetting> Spellbooks = new Dictionary<string, SpellbookSetting>(StringComparer.OrdinalIgnoreCase);

        public static string FilePath => Path.Combine(Main.ModEntry.Path, "settings.json");

        public static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var loaded = JsonConvert.DeserializeObject<Settings>(json);
                    if (loaded != null)
                    {
                        loaded.Spellbooks = loaded.Spellbooks ?? new Dictionary<string, SpellbookSetting>(StringComparer.OrdinalIgnoreCase);
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to load settings.json: {ex}");
            }

            return new Settings();
        }

        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to save settings.json: {ex}");
            }
        }

        public SpellbookSetting GetOrCreate(string guid)
        {
            if (!Spellbooks.TryGetValue(guid, out var setting) || setting == null)
            {
                setting = new SpellbookSetting();
                Spellbooks[guid] = setting;
            }

            return setting;
        }
    }
}
