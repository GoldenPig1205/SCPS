using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Exiled.API.Features;
using Exiled.API.Features.Components;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using MEC;
using SCPSLAudioApi.AudioCore;
using VoiceChat;
using Exiled.API.Features.Roles;

namespace SCPS
{
    public class Gtool
    {
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
            ReferenceHub npc = SCPS.Instance.Chracters.Find(x => x.Name == Name).npc;
            AudioPlayerBase audio = AudioPlayerBase.Get(npc);
            audio.BroadcastChannel = BroadcastChannel;
            audio.CurrentPlay = ConventToAudioPath(AudioFileName);
            audio.Volume = Volume;
            audio.Loop = Loop;
            audio.Play(-1);
        }

        public static void ClearSound(string Name)
        {
            ReferenceHub npc = SCPS.Instance.Chracters.Find(x => x.Name == Name).npc;
            AudioPlayerBase audio = AudioPlayerBase.Get(npc);
            audio.Stoptrack(true);
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

        public static ReferenceHub Spawn(RoleTypeId role, Vector3 pos)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(NetworkManager.singleton.playerPrefab);
            ReferenceHub hub = gameObject.GetComponent<ReferenceHub>();
            try
            {
                hub.roleManager.InitializeNewRole(RoleTypeId.None, RoleChangeReason.None, RoleSpawnFlags.All, null);
            }
            catch { }
            int id = new RecyclablePlayerId(true).Value;
            FakeConnection fakeConnection = new FakeConnection(id);
            NetworkServer.AddPlayerForConnection(fakeConnection, gameObject);
            Timing.CallDelayed(0.25f, () =>
            {
                try
                {
                    hub.roleManager.ServerSetRole(role, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                }
                catch { }
            });
            Timing.CallDelayed(0.35f, () =>
            {
                // hub.nicknameSync.DisplayName = role.ToString();
                hub.nicknameSync.Network_myNickSync = role.ToString();
                string name = "ID_Dedicated";
                hub.authManager.UserId = name;
                hub.authManager.NetworkSyncedUserId = name;
                hub.TryOverridePosition(pos + Vector3.up * 1.5f, Vector3.zero);
            });
            Npc npc = new Npc(gameObject)
            {
                IsNPC = true,
            };
            Player.Dictionary.Add(gameObject, npc);
            return hub;
        }

        public static void Register(ReferenceHub Chracters, string Name)
        {
            try
            {
                Chracters chracters = new Chracters { Name = Name, npc = Chracters };
                SCPS.Instance.Chracters.Add(chracters);
                HideFromList(Chracters);
            }
            catch (Exception ex) { }
        }

        public static Player PlayerGet(string Name)
        {
            return Player.Get(SCPS.Instance.Chracters.Find(x => x.Name == Name).npc.PlayerId);
        }
    }
}
