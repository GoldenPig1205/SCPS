using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Exiled.API.Features;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using VoiceChat;
using Exiled.API.Features.Roles;
using SCPS.Locations;

namespace SCPS
{
    public class Gtool
    {
        private const string MainSpeakerName = "Main";
        private const string GlobalAudioName = "Global";
        private static readonly Dictionary<string, AudioPlayer> AudioPlayers =
            new Dictionary<string, AudioPlayer>(StringComparer.OrdinalIgnoreCase);

        public static object GetRandomValue(List<object> list)
        {
            System.Random random = new System.Random();
            int index = random.Next(0, list.Count);
            return list[index];
        }

        public static string ConventToAudioPath(string filename)
        {
            return Paths.Plugins + $"/audio/{filename}.ogg";
        }

        public static Room CameraRoom()
        {
            Player s0p = Player.List.ToList().Find(x => x.Role.Type == RoleTypeId.Scp079);

            if (s0p != null && s0p.Role is Scp079Role scp079)
                return scp079.Camera.Room;

            else
                return null;
        } 

        public static void PlaySound(string Name, string AudioFileName, VoiceChatChannel BroadcastChannel = VoiceChatChannel.Proximity, int Volume = 100, bool Loop = false)
        {
            Chracters character = SCPS.Instance?.Chracters.Find(x => x.Name == Name);
            if (character?.npc == null)
                return;

            string clipPath = ConventToAudioPath(AudioFileName);
            if (!File.Exists(clipPath))
            {
                Log.Warn($"[SCPS] Audio file was not found: {clipPath}");
                return;
            }

            if (!AudioClipStorage.AudioClips.ContainsKey(AudioFileName))
                AudioClipStorage.LoadClip(clipPath, AudioFileName);

            AudioPlayer audio = GetOrCreateAudioPlayer(Name, character.npc);
            if (audio == null)
                return;

            if (audio.TryGetSpeaker(MainSpeakerName, out Speaker speaker))
                speaker.IsSpatial = BroadcastChannel != VoiceChatChannel.Intercom;

            audio.RemoveAllClips();
            audio.AddClip(AudioFileName, Math.Max(0f, Volume / 100f), Loop, !Loop);
        }

        public static void PlayGlobalSound(string audioFileName, int volume = 100, bool loop = false)
        {
            string clipPath = ConventToAudioPath(audioFileName);
            if (!File.Exists(clipPath))
            {
                Log.Warn($"[SCPS] Audio file was not found: {clipPath}");
                return;
            }

            if (!AudioClipStorage.AudioClips.ContainsKey(audioFileName))
                AudioClipStorage.LoadClip(clipPath, audioFileName);

            AudioPlayer audio = GetOrCreateGlobalAudioPlayer();
            if (audio == null)
                return;

            audio.RemoveAllClips();
            audio.AddClip(audioFileName, Math.Max(0f, volume / 100f), loop, !loop);
        }

        public static void ClearGlobalSound()
            => ClearSound(GlobalAudioName);

        public static void ClearSound(string Name)
        {
            if (AudioPlayers.TryGetValue(Name, out AudioPlayer audio))
                audio.RemoveAllClips();
        }

        public static void ClearAllSounds()
        {
            foreach (AudioPlayer audio in AudioPlayers.Values)
                audio?.Destroy();

            AudioPlayers.Clear();
        }

        private static AudioPlayer GetOrCreateAudioPlayer(string name, ReferenceHub npc)
        {
            if (AudioPlayers.TryGetValue(name, out AudioPlayer existing) && existing != null)
                return existing;

            Transform source = npc.transform;
            AudioPlayer created = AudioPlayer.CreateOrGet(
                $"SCPS - {name}",
                condition: hub => Player.Get(hub) != null,
                onIntialCreation: player =>
                {
                    player.transform.parent = source;
                    Speaker speaker = player.AddSpeaker(
                        MainSpeakerName,
                        isSpatial: true,
                        minDistance: 1f,
                        maxDistance: 5000f);
                    speaker.transform.parent = source;
                    speaker.transform.localPosition = Vector3.zero;
                });

            AudioPlayers[name] = created;
            return created;
        }

        private static AudioPlayer GetOrCreateGlobalAudioPlayer()
        {
            if (AudioPlayers.TryGetValue(GlobalAudioName, out AudioPlayer existing) && existing != null)
                return existing;

            AudioPlayer created = AudioPlayer.CreateOrGet(
                "SCPS - Global",
                condition: hub => Player.Get(hub) != null,
                onIntialCreation: audio =>
                {
                    audio.AddSpeaker(
                        MainSpeakerName,
                        isSpatial: false,
                        minDistance: 0f,
                        maxDistance: 5000f);
                });

            AudioPlayers[GlobalAudioName] = created;
            return created;
        }

        public static void HideFromList(ReferenceHub PlayerDummy)
        {
            PlayerDummy.authManager.NetworkSyncedUserId = "ID_Dedicated";
        }

        public static void Rotate(ReferenceHub npc, Vector3 vector3)
        {
            Vector3 direction = vector3;
            Quaternion quat = Quaternion.LookRotation(direction, Vector3.up);
            FpcMouseLook mouseLook = (npc.roleManager.CurrentRole as FpcStandardRoleBase).FpcModule.MouseLook;
            (ushort horizontal, ushort vertical) = quat.ToClientUShorts();
            mouseLook.ApplySyncValues(horizontal, vertical);
        }

        public static void Rotate(ReferenceHub npc, Quaternion rotation)
        {
            if (!(npc?.roleManager.CurrentRole is FpcStandardRoleBase role))
                return;
            FpcMouseLook mouseLook = role.FpcModule.MouseLook;
            (ushort horizontal, ushort vertical) = rotation.ToClientUShorts();
            mouseLook.ApplySyncValues(horizontal, vertical);
        }

        public static void Place(ReferenceHub npc, ScpsLocation location)
        {
            if (npc == null || location == null)
                return;
            npc.TryOverridePosition(location.Position);
            Rotate(npc, location.Quaternion);
        }

        public static void Place(Player player, ScpsLocation location)
        {
            if (player == null || location == null)
                return;
            player.Position = location.Position;
            player.Rotation = location.Quaternion;
        }

        public static ReferenceHub Spawn(RoleTypeId role, ScpsLocation location)
        {
            if (location == null)
                return null;
            Npc npc = Npc.Spawn(role.ToString(), role, ignored: true, position: location.Position);
            if (npc?.ReferenceHub != null)
                Rotate(npc.ReferenceHub, location.Quaternion);
            return npc?.ReferenceHub;
        }

        public static ReferenceHub Spawn(RoleTypeId role, Vector3 pos)
        {
            Npc npc = Npc.Spawn(
                role.ToString(),
                role,
                ignored: true,
                position: pos + Vector3.up * 1.5f);

            return npc?.ReferenceHub;
        }

        public static void Register(ReferenceHub Chracters, string Name)
        {
            try
            {
                Chracters chracters = new Chracters { Name = Name, npc = Chracters };
                SCPS.Instance.Chracters.Add(chracters);
                HideFromList(Chracters);
            }
            catch (Exception ex)
            {
                Log.Error($"[SCPS] Failed to register character '{Name}': {ex}");
            }
        }

        public static Player PlayerGet(string Name)
        {
            return Player.Get(SCPS.Instance.Chracters.Find(x => x.Name == Name).npc.PlayerId);
        }
    }
}
