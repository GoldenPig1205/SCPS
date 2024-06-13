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

namespace SCPS
{
    public class Gtool
    {
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
    }
}
