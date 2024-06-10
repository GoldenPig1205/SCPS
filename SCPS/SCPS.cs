/* SCPS (ver. Alpha 0.0.1) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using UnityEngine;

namespace SCPS
{
    public class SCPS : Plugin<Config>
    {
        public static SCPS Instance;
        public Player player = null;

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

            Instance = null;
        }

        public async void OnWaitingForPlayers()
        {
            while (Player.List.Count < 1)
                await Task.Delay(1000);

            player = Player.List.ToList()[0];

            Map.CleanAllItems();

            Round.IsLocked = true;
            Round.Start();

            
        }

        public void OnRoundStarted()
        {
            player.Position = new Vector3(68.85482f, -1002.415f, 54.73438f);
            player.EnableEffect(Exiled.API.Enums.EffectType.Ensnared);

            var PlayerDummy = Npc.Spawn(player.Nickname, PlayerRoles.RoleTypeId.ClassD, userId: "PlayerDummy");

            PlayerDummy.ReferenceHub.authManager.NetworkSyncedUserId = "ID_Dedicated";
            PlayerDummy.ReferenceHub.authManager.syncMode = (SyncMode)ClientInstanceMode.DedicatedServer;
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
            ServerConsole.AddLog($"{ev.Player.Nickname}의 방향 : new Quaternion({ev.Player.Rotation.x}f, {ev.Player.Rotation.y}f, {ev.Player.Rotation.z}f, {ev.Player.Rotation.w}f)", ConsoleColor.Blue);
        }

        public void OnActivatingWorkstation(Exiled.Events.EventArgs.Player.ActivatingWorkstationEventArgs ev)
        {
            ev.IsAllowed = false;

            ev.Player.Role.Set(PlayerRoles.RoleTypeId.Scp079);

            foreach (var Npc in Npc.List)
            {
                if (Npc.UserId == "PlayerDummy")
                    Npc.Position = new Vector3(68.85482f, -1002.415f, 54.73438f);
            }
        }

        public void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            foreach (var Npc in Npc.List)
            {
                if (Npc.UserId == "PlayerDummy")
                    Npc.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            }

            player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            player.Position = new Vector3(68.85482f, -1002.415f, 54.73438f);
            player.EnableEffect(Exiled.API.Enums.EffectType.Ensnared);
        }

    }
}

