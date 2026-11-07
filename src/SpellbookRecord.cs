using System.Collections.Generic;
using Kingmaker.Blueprints.Classes.Spells;

namespace allSpontaneous
{
    public sealed class SpellbookRecord
    {
        public string Guid;
        public string SpellbookInternalName;
        public BlueprintSpellbook Spellbook;
        public readonly List<string> Owners = new List<string>();

        public string OwnerLabel => Owners.Count == 0 ? SpellbookInternalName : string.Join(", ", Owners);

        public bool IsPrepared => Spellbook != null && !Spellbook.Spontaneous && !Spellbook.IsArcanist;

        public bool IsConvertedByUs =>
            Main.Settings != null
            && Main.Settings.Spellbooks.TryGetValue(Guid, out var setting)
            && setting != null
            && setting.Converted;

        public bool IsNativeArcanist
        {
            get
            {
                if (Spellbook == null || !Spellbook.IsArcanist)
                {
                    return false;
                }

                if (Main.Settings != null
                    && Main.Settings.Spellbooks.TryGetValue(Guid, out var setting)
                    && setting != null
                    && setting.CapturedOriginals)
                {
                    return setting.OriginalIsArcanist;
                }

                return !IsConvertedByUs;
            }
        }
    }
}
