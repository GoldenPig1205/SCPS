/* SCPS (ver. Alpha 0.0.1) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using Exiled.API.Enums;
using UnityEngine;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using MEC;

namespace SCPS
{
    public class SCPS : Plugin<Config>
    {
        public static SCPS Instance;

        public Player player = null;
        public List<Chracters> Chracters = new List<Chracters>();

        public bool sync = false;
        public bool IsEnd = false;
        public float Battery = 100;
        public List<string> Using = new List<string>() { "RedLightOnSR" };

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
            Exiled.Events.Handlers.Player.ActivatingWorkstation += OnActivatingWorkstation;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;

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
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;

            Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= OnInteractingTesla;
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= OnTriggeringDoor;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting -= OnElevatorTeleporting;

            Instance = null;
        }

        public async void OnWaitingForPlayers()
        {
            Map.CleanAllItems();
            Server.ExecuteCommand("/mp load SCPS");

            while (Player.List.Count < 1)
                await Task.Delay(1000);

            player = Player.List.ToList()[0];
            Round.IsLocked = true;
            Server.ExecuteCommand($"/decontamination disable");

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

            Round.Start();
        }

        public async void OnRoundStarted()
        {
            player.Role.Set(RoleTypeId.FacilityGuard);
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);
            Map.TurnOffAllLights(99999);

            ReferenceHub PlayerDummy = Gtool.Spawn(RoleTypeId.FacilityGuard, new Vector3(46.32286f, 0.91f, 64.23f));
            ReferenceHub Scp049 = Gtool.Spawn(RoleTypeId.Scp049, new Vector3(38.65023f, -806.6f, 81.84583f));
            ReferenceHub Scp939 = Gtool.Spawn(RoleTypeId.Scp939, new Vector3(98.94531f, -998.655f, 93.27344f));

            Dictionary<ReferenceHub, string> register = new Dictionary<ReferenceHub, string>()
            {
                { PlayerDummy, "PlayerDummy" }, { Scp049, "Scp049" }, { Scp939, "Scp939" }
            };

                foreach (var reg in register)
                    Gtool.Register(reg.Key, reg.Value);

            Tasks.Instance = new Tasks();

            await Task.WhenAll
            (
                Tasks.Instance.Sync079andBattery(),
                Tasks.Instance.Timer(),
                Tasks.Instance.UsingBattery(),
                Tasks.Instance.ShowBattery(),

                Tasks.Instance.Scp049(20),
                Tasks.Instance.Scp939(20)
            );
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

            if (!(Battery < 0.3f))
            {
                ev.Player.Role.Set(RoleTypeId.Scp079);

                ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
                pd.TryOverridePosition(new Vector3(68.2181f, -1002.403f, 54.75781f), Vector3.zero);
                Gtool.Rotate(pd, new Vector3(1, -2, 2));

                Using.Add("CCTV");
            }
        }

        public async void OnSearchingPickup(Exiled.Events.EventArgs.Player.SearchingPickupEventArgs ev)
        {
            if (ev.Pickup.Type == ItemType.KeycardScientist)
            {
                if (!Using.Contains("Light1"))
                {
                    Using.Add("Light1");
                    await Task.Delay(2000);
                    Using.Remove("Light1");
                }
            }

            else if (ev.Pickup.Type == ItemType.KeycardResearchCoordinator)
            {
                if (!Using.Contains("Light2"))
                {
                    Using.Add("Light2");
                    await Task.Delay(2000);
                    Using.Remove("Light2");
                }
            }
        }

        public void OnInteractingDoor(Exiled.Events.EventArgs.Player.InteractingDoorEventArgs ev)
        {
            if (!(Battery < 0.3f))
            {
                if (ev.Door.IsOpen)
                    Using.Add("DoorClose");
                else
                    Using.Remove("DoorClose");
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            if (ev.Player.Role.Type == RoleTypeId.FacilityGuard)
                ev.Player.ClearInventory();
        }

        public void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
            pd.TryOverridePosition(new Vector3(46.32286f, 0.91f, 64.23f), Vector3.zero);

            player.Role.Set(RoleTypeId.FacilityGuard);
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);

            Using.Remove("CCTV");
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

