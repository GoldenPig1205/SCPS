using System;

namespace SCPS.Progress
{
    public sealed class ScpsPlayerProgress
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public long Revision { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string LastKnownNickname { get; set; } = string.Empty;
        public int HighestClearedNight { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsCustomUnlocked => HighestClearedNight >= 6;

        public bool IsNightUnlocked(int night)
            => night >= 1 && night <= 6 && night <= Math.Min(6, HighestClearedNight + 1);

        public bool MarkCleared(int night)
        {
            if (!IsNightUnlocked(night) || night <= HighestClearedNight)
                return false;

            HighestClearedNight = Math.Max(HighestClearedNight, Math.Min(6, night));
            return true;
        }

        public void Normalize()
        {
            SchemaVersion = CurrentSchemaVersion;
            HighestClearedNight = Math.Max(0, Math.Min(6, HighestClearedNight));
            UserId ??= string.Empty;
            LastKnownNickname ??= string.Empty;
        }
    }
}
