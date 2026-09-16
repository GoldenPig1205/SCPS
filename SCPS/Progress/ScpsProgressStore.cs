using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace SCPS.Progress
{
    public sealed class ScpsProgressStore
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        private readonly ConcurrentDictionary<string, ScpsPlayerProgress> profiles =
            new ConcurrentDictionary<string, ScpsPlayerProgress>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, object> profileLocks =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public ScpsProgressStore(string rootDirectory)
        {
            RootDirectory = Path.Combine(rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory)), "Data", "Players");
        }

        public string RootDirectory { get; }

        public void Initialize() => Directory.CreateDirectory(RootDirectory);

        public ScpsPlayerProgress LoadOrCreate(string userId, string nickname)
        {
            ValidateUserId(userId);
            object gate = GetLock(userId);
            lock (gate)
            {
                if (profiles.TryGetValue(userId, out ScpsPlayerProgress cached))
                {
                    cached.LastKnownNickname = nickname ?? cached.LastKnownNickname;
                    return cached;
                }

                ScpsPlayerProgress profile;
                string path = GetProfilePath(userId);
                if (!File.Exists(path))
                {
                    profile = Create(userId, nickname);
                    profiles[userId] = profile;
                    SaveLocked(profile);
                    return profile;
                }

                try
                {
                    profile = Deserialize(File.ReadAllText(path, Encoding.UTF8), userId);
                }
                catch (Exception primaryException)
                {
                    string backupPath = GetBackupPath(userId);
                    if (!File.Exists(backupPath))
                        throw new InvalidDataException($"SCPS progress '{userId}' is corrupt and has no backup.", primaryException);

                    profile = Deserialize(File.ReadAllText(backupPath, Encoding.UTF8), userId);
                    Log.Warn($"[SCPS] Restored progress for '{userId}' from backup.");
                }

                profile.LastKnownNickname = nickname ?? profile.LastKnownNickname;
                profiles[userId] = profile;
                return profile;
            }
        }

        public ScpsPlayerProgress Get(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            return LoadOrCreate(player.UserId, player.Nickname);
        }

        public bool MarkNightCleared(Player player, int night)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId))
                return false;

            object gate = GetLock(player.UserId);
            lock (gate)
            {
                ScpsPlayerProgress profile = LoadOrCreate(player.UserId, player.Nickname);
                if (!profile.MarkCleared(night))
                    return false;

                SaveLocked(profile);
                return true;
            }
        }

        public void SaveAndUnload(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !profiles.ContainsKey(userId))
                return;

            object gate = GetLock(userId);
            lock (gate)
            {
                if (profiles.TryGetValue(userId, out ScpsPlayerProgress profile))
                    SaveLocked(profile);
                profiles.TryRemove(userId, out _);
                profileLocks.TryRemove(userId, out _);
            }
        }

        public void SaveAll()
        {
            foreach (string userId in profiles.Keys.ToArray())
            {
                try
                {
                    object gate = GetLock(userId);
                    lock (gate)
                    {
                        if (profiles.TryGetValue(userId, out ScpsPlayerProgress profile))
                            SaveLocked(profile);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error($"[SCPS] Failed to save progress for '{userId}': {exception}");
                }
            }
        }

        private void SaveLocked(ScpsPlayerProgress profile)
        {
            profile.Normalize();
            profile.UpdatedAtUtc = DateTime.UtcNow;
            profile.Revision++;

            string directory = GetPlayerDirectory(profile.UserId);
            string path = GetProfilePath(profile.UserId);
            string backupPath = GetBackupPath(profile.UserId);
            string temporaryPath = path + ".tmp";
            Directory.CreateDirectory(directory);

            byte[] content = new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(profile, SerializerSettings));
            using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(content, 0, content.Length);
                stream.Flush(true);
            }

            Deserialize(File.ReadAllText(temporaryPath, Encoding.UTF8), profile.UserId);
            if (!File.Exists(path))
            {
                File.Move(temporaryPath, path);
                return;
            }

            try
            {
                File.Replace(temporaryPath, path, backupPath, true);
            }
            catch (PlatformNotSupportedException)
            {
                ReplacePortable(temporaryPath, path, backupPath);
            }
            catch (IOException)
            {
                ReplacePortable(temporaryPath, path, backupPath);
            }
        }

        private static void ReplacePortable(string temporaryPath, string path, string backupPath)
        {
            File.Copy(path, backupPath, true);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        private static ScpsPlayerProgress Create(string userId, string nickname)
        {
            DateTime now = DateTime.UtcNow;
            return new ScpsPlayerProgress
            {
                UserId = userId,
                LastKnownNickname = nickname ?? string.Empty,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
        }

        private static ScpsPlayerProgress Deserialize(string json, string expectedUserId)
        {
            ScpsPlayerProgress profile = JsonConvert.DeserializeObject<ScpsPlayerProgress>(json, SerializerSettings)
                ?? throw new InvalidDataException("SCPS progress JSON produced a null object.");
            if (!string.Equals(profile.UserId, expectedUserId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SCPS progress user ID does not match its directory.");
            if (profile.SchemaVersion > ScpsPlayerProgress.CurrentSchemaVersion)
                throw new InvalidDataException($"SCPS progress schema {profile.SchemaVersion} is newer than supported.");
            profile.Normalize();
            return profile;
        }

        private object GetLock(string userId)
        {
            ValidateUserId(userId);
            return profileLocks.GetOrAdd(userId, _ => new object());
        }

        private string GetPlayerDirectory(string userId) => Path.Combine(RootDirectory, SanitizeUserId(userId));
        private string GetProfilePath(string userId) => Path.Combine(GetPlayerDirectory(userId), "progress.json");
        private string GetBackupPath(string userId) => Path.Combine(GetPlayerDirectory(userId), "progress.backup.json");

        private static string SanitizeUserId(string userId)
        {
            StringBuilder builder = new StringBuilder(userId.Length);
            foreach (char character in userId)
            {
                if (char.IsLetterOrDigit(character) || character == '@' || character == '.' || character == '_' || character == '-')
                    builder.Append(character);
                else
                    builder.Append('_');
            }
            return builder.ToString();
        }

        private static void ValidateUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("A non-empty authenticated user ID is required.", nameof(userId));
        }
    }
}
