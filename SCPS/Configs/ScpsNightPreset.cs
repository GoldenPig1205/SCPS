using System;
using System.Collections.Generic;
using System.Linq;

namespace SCPS
{
    public sealed class ScpsNightPreset
    {
        public int Night { get; set; }

        public Dictionary<string, int> Levels { get; set; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public int GetLevel(string scpName)
        {
            if (Levels == null || string.IsNullOrWhiteSpace(scpName) || !Levels.TryGetValue(scpName, out int level))
                return 0;

            return Math.Max(0, Math.Min(20, level));
        }

        public static List<ScpsNightPreset> CreateDefaults()
        {
            return new List<ScpsNightPreset>
            {
                Create(1, 2, 1, 0, 0, 0, 0, 1),
                Create(2, 3, 2, 1, 0, 1, 0, 2),
                Create(3, 4, 3, 2, 1, 2, 1, 3),
                Create(4, 6, 5, 4, 3, 4, 2, 5),
                Create(5, 8, 7, 6, 5, 6, 4, 7),
                Create(6, 10, 9, 8, 7, 8, 6, 10),
            };
        }

        public static ScpsNightPreset Find(IEnumerable<ScpsNightPreset> presets, int night)
        {
            return (presets ?? Enumerable.Empty<ScpsNightPreset>())
                .FirstOrDefault(preset => preset != null && preset.Night == night);
        }

        private static ScpsNightPreset Create(
            int night,
            int scp049,
            int scp939,
            int scp0492,
            int scp106,
            int scp3114,
            int scp096,
            int scp173)
        {
            return new ScpsNightPreset
            {
                Night = night,
                Levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SCP-049"] = scp049,
                    ["SCP-939"] = scp939,
                    ["SCP-049-2"] = scp0492,
                    ["SCP-106"] = scp106,
                    ["SCP-3114"] = scp3114,
                    ["SCP-096"] = scp096,
                    ["SCP-173"] = scp173,
                },
            };
        }
    }
}
