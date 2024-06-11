/* SCPS (ver. Alpha 0.0.1) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Exiled.API.Features;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Components;

using UnityEngine;

using CentralAuth;
using Mirror;


using PlayerRoles.FirstPersonControl;
using PlayerRoles;

using MEC;
using Footprinting;

namespace SCPS
{
    public class Chracters
    {
        public ReferenceHub npc;
        public string Name;
    }

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
                //hub.nicknameSync.DisplayName = role.ToString();
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
    }

    public static class Extensions
    {
        public static (ushort horizontal, ushort vertical) ToClientUShorts(this Quaternion rotation)
        {
            const float ToHorizontal = ushort.MaxValue / 360f;
            const float ToVertical = ushort.MaxValue / 176f;

            float fixVertical = -rotation.eulerAngles.x;

            if (fixVertical < -90f)
            {
                fixVertical += 360f;
            }
            else if (fixVertical > 270f)
            {
                fixVertical -= 360f;
            }

            float horizontal = Mathf.Clamp(rotation.eulerAngles.y, 0f, 360f);
            float vertical = Mathf.Clamp(fixVertical, -88f, 88f) + 88f;

            return ((ushort)Math.Round(horizontal * ToHorizontal), (ushort)Math.Round(vertical * ToVertical));
        }
    }

    public class SCPS : Plugin<Config>
    {
        public static SCPS Instance;

        public Player player = null;
        public List<Chracters> Chracters = new List<Chracters>();

        public bool sync = false;
        public int Battery = 100;
        public List<string> Using = new List<string>();

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
            Exiled.Events.Handlers.Player.ActivatingWorkstation += OnActivatingWorkstation;

            Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
            Exiled.Events.Handlers.Scp079.InteractingTesla += OnInteractingTesla;
            Exiled.Events.Handlers.Scp079.TriggeringDoor += OnTriggeringDoor;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting += OnElevatorTeleporting;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;
            Exiled.Events.Handlers.Player.ActivatingWorkstation -= OnActivatingWorkstation;

            Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= OnInteractingTesla;
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= OnTriggeringDoor;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting -= OnElevatorTeleporting;

            Instance = null;
        }

        public async void OnWaitingForPlayers()
        {
            Server.ExecuteCommand("/mp load SCPS");

            while (Player.List.Count < 1)
                await Task.Delay(1000);

            player = Player.List.ToList()[0];

            Map.CleanAllItems();

            Round.IsLocked = true;
            Round.Start();

            foreach (var window in Window.List)
            {
                if (window.Room.name == "HCZ_079" && (window.Base.name == "Glass (1)" || window.Base.name == "Glass (2)"))
                {
                    window.IsBroken = true;

                    foreach (var door in window.Room.Doors)
                    {
                        door.Unlock();
                        door.IsOpen = true;
                    }
                }
            }

            foreach (var room in Room.List)
            {
                room.AreLightsOff = true;
                room.Doors.ToList().ForEach(x => x.IsOpen = true);
            }
        }

        public async void OnRoundStarted()
        {
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);
            Map.TurnOffAllLights(99999);

            ReferenceHub PlayerDummy = Gtool.Spawn(RoleTypeId.ClassD, new Vector3(46.32286f, 0.91f, 64.23f));

            Timing.CallDelayed(1f, () => 
            { 
                Chracters chracters = new Chracters { Name = "PlayerDummy", npc = PlayerDummy }; 
                Chracters.Add(chracters); 
                Gtool.HideFromList(PlayerDummy); 
            });


            while (true)
            {
                foreach (var scp in Player.List.Where(x => x.Role.Type == RoleTypeId.Scp079))
                {
                    if (scp.Role is Exiled.API.Features.Roles.Scp079Role scp079)
                        scp079.Energy = Battery;
                }

                await Task.Delay(100);
            }
        }

        public void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev)
        {
            Server.ExecuteCommand("sr");
        }

        public void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            if (ev.Player == player)
                Server.ExecuteCommand("sr");
        }

        public void OnFlippingCoin(Exiled.Events.EventArgs.Player.FlippingCoinEventArgs ev)
        {
            ServerConsole.AddLog($"{ev.Player.Nickname}의 위치 : new Vector3({ev.Player.Position.x}f, {ev.Player.Position.y}f, {ev.Player.Position.z}f)", ConsoleColor.DarkMagenta);
        }

        public void OnActivatingWorkstation(Exiled.Events.EventArgs.Player.ActivatingWorkstationEventArgs ev)
        {
            ev.IsAllowed = false;

            ev.Player.Role.Set(RoleTypeId.Scp079);

            ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
            pd.TryOverridePosition(new Vector3(68.2181f, -1002.403f, 54.75781f), Vector3.zero);
        }

        public void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
            pd.TryOverridePosition(new Vector3(46.32286f, 0.91f, 64.23f), Vector3.zero);

            player.Role.Set(RoleTypeId.ClassD);
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);
        }

        public void OnElevatorTeleporting(Exiled.Events.EventArgs.Scp079.ElevatorTeleportingEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnInteractingTesla(Exiled.Events.EventArgs.Scp079.InteractingTeslaEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnTriggeringDoor(Exiled.Events.EventArgs.Scp079.TriggeringDoorEventArgs ev)
        {
            ev.IsAllowed = false;
        }
    }
}

