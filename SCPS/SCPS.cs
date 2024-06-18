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
using SCPSLAudioApi.AudioCore;
using VoiceChat;
using Exiled.API.Features.Items;
using CustomPlayerEffects;

namespace SCPS
{
    public class SCPS : Plugin<Config>
    {
        public static SCPS Instance;

        public Player player = null;
        public List<Chracters> Chracters = new List<Chracters>();

        public bool sync = false;
        public bool IsEnd = false;
        public bool IsFemur = false;
        public bool IsLookedatScp096 = false;
        public bool IsCCTV = false;
        public float Battery = 100;
        public string Killer = null;
        public Dictionary<string, int> SetLevel = new Dictionary<string, int>() 
        { 
            { "SCP-049", 0 }, { "SCP-939", 0 }, { "SCP-049-2", 0 }, { "SCP-106", 0 }, { "SCP-3114", 0 },
            { "SCP-096", 0 }, { "SCP-173", 0 }
        };
        public Dictionary<string, string> Method = new Dictionary<string, string>()
        {
            { "SCP-049", "난이도 : ★☆☆☆☆\n역병 의사라 불리는 그는 좌측 문에서 공격해올 것입니다. 가끔씩 혼잣말을 하거나 구두 소리를 냅니다. CCTV에서 빨간 점으로 표시되어 위치를 파악하기 쉽습니다." }, 
            { "SCP-939", "난이도 : ★★☆☆☆\n소리 없는 암살자입니다. 좌측 문에서 대기할 때 숨소리가 들립니다." }, 
            { "SCP-049-2", "난이도 : ★★☆☆☆\n앞쪽 환풍구를 통해서 당신에게 서서히 도달할 것입니다. 쓰러진 척 하는 연기가 속지 마십시오." }, 
            { "SCP-106", "난이도 : ★★★☆☆\n천천히 당신을 향해서 접근할 것입니다. 그는 시설 벽을 뚫고 당신에게 도달할 수 있습니다. 그를 막을 유일한 방법은, 그가 당신의 사무실에서 당신을 관찰하고 있을 때, 재빨리 CCTV를 SCP-106의 격리실로 옮긴 후, 스피커(v키)를 활성화하십시오." },
            { "SCP-3114", "난이도 : ★★☆☆☆\n당신의 채취를 쫒아 오른쪽 환풍구로 도달할 것입니다. 그가 사무실에 나타났을 때 CCTV를 쳐다보십시오. 인간인 것을 들키지 않아야 합니다!" },
            { "SCP-096", "난이도 : ★★★★☆\n그는 당신이 그의 얼굴을 \"확인\"하기 전까지는 절대로 해치지 않습니다. CCTV로 그를 발견했을 경우 최대한 빠르게 우회하십시오." },
            { "SCP-173", "난이도 : ★★★★★\n매우 재빠른 이 개체는 당신의 사무실로 돌진까지 4단계의 준비 과정이 있습니다. 그가 자신의 방을 떠난 경우 최대한 빠르게 문을 닫으십시오." }
        };
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
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Died += OnDied;

            Exiled.Events.Handlers.Scp079.Pinging += OnPinging;
            Exiled.Events.Handlers.Scp079.InteractingTesla += OnInteractingTesla;
            Exiled.Events.Handlers.Scp079.TriggeringDoor += OnTriggeringDoor;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting += OnElevatorTeleporting;
            Exiled.Events.Handlers.Scp079.ChangingSpeakerStatus += OnChangingSpeakerStatus;
            Exiled.Events.Handlers.Scp079.ChangingCamera += OnChangingCamera;

            Exiled.Events.Handlers.Scp096.AddingTarget += OnAddingTarget;
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
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Died -= OnDied;

            Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;
            Exiled.Events.Handlers.Scp079.InteractingTesla -= OnInteractingTesla;
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= OnTriggeringDoor;
            Exiled.Events.Handlers.Scp079.ElevatorTeleporting -= OnElevatorTeleporting;
            Exiled.Events.Handlers.Scp079.ChangingSpeakerStatus -= OnChangingSpeakerStatus;
            Exiled.Events.Handlers.Scp079.ChangingCamera -= OnChangingCamera;

            Exiled.Events.Handlers.Scp096.AddingTarget -= OnAddingTarget;

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

            ReferenceHub PlayerDummy = Gtool.Spawn(RoleTypeId.FacilityGuard, new Vector3(46.32286f, 0.91f, 64.23f));
            ReferenceHub Scp049 = Gtool.Spawn(RoleTypeId.Scp049, new Vector3(38.65023f, -806.6f, 81.84583f));
            ReferenceHub Scp049Dummy = Gtool.Spawn(RoleTypeId.ClassD, new Vector3(38.65023f, -806.6f, 81.84583f));
            ReferenceHub Scp939 = Gtool.Spawn(RoleTypeId.Scp939, new Vector3(98.94531f, -998.655f, 93.27344f));
            ReferenceHub PhoneGuy = Gtool.Spawn(RoleTypeId.ClassD, new Vector3(46.32286f, 0.91f, 64.23f));
            ReferenceHub Scp106 = Gtool.Spawn(RoleTypeId.Scp106, new Vector3(28.48828f, -998.7513f, 152.0195f));
            ReferenceHub Scp3114 = Gtool.Spawn(RoleTypeId.Scp3114, new Vector3(59f, -1004.276f, 67.01563f));
            ReferenceHub Scp096 = Gtool.Spawn(RoleTypeId.Scp096, new Vector3(90.01107f, -999.0436f, 133.1367f));
            ReferenceHub Scp173 = Gtool.Spawn(RoleTypeId.Scp173, new Vector3(46.17308f, -802.235f, 96.46692f));

            Scp049Dummy.transform.localScale = Vector3.one * -0.01f;

            foreach (Player player in Player.List)
            {
                Server.SendSpawnMessage.Invoke(null, new object[]
                    {
                        Scp049Dummy.netIdentity,
                        player.Connection
                    }
                );
            }

            Dictionary<ReferenceHub, string> register = new Dictionary<ReferenceHub, string>()
            {
                { PlayerDummy, "PlayerDummy" }, { Scp049, "Scp049" }, { Scp049Dummy, "Scp049Dummy" }, { Scp939, "Scp939" }, { PhoneGuy, "PhoneGuy" },
                { Scp106, "Scp106" }, { Scp3114, "Scp3114" }, { Scp096, "Scp096" }, { Scp173, "Scp173" }
            };

            foreach (var reg in register)
                Gtool.Register(reg.Key, reg.Value);

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

            Player.Get(11).DisplayNickname = "BGM";
            Gtool.PlaySound("PhoneGuy", $"bgm-{UnityEngine.Random.Range(1, 8)}", VoiceChatChannel.Intercom, 30, Loop: true);

            bool broadcast = false;
            while (Round.IsLobby)
            {
                if (Player.List.Count > 0)
                {
                    if (!broadcast)
                    {
                        player.Broadcast(300, "<size=20><b>당신은 <color=red>SCP-079</color>의 전력을 제한하고 게이트를 여는 등 희생을 자처했지만, 모두가 탈출한 <color=#BDBDBD>Site-02</color> 기지에 홀로 버려졌습니다.\n" +
                                               "이제 시설에 남겨진 것은 <u><color=#FACC2E>제한된 전력</color></u>과 <i>당신의 친구 <color=red>SCP-079</color></i>, <u><color=#58ACFA>사무실</color></u> 뿐입니다.\n" +
                                               "약속된 지원의 시간은 새벽 6시, 그때까지 최대한 버텨내야만 합니다.</b></size>");
                        broadcast = true;
                    }

                    string output = "";
                    foreach (var item in SetLevel)
                    {
                        output += $"<color=red>{item.Key}</color> : {item.Value}\n";
                    }
                    output = output.TrimEnd('\n');
                    player.ShowHint($"<align=left><b><size=50>A.I. Level</size></b>\n{output}</align>\n\n콘솔(~)을 열고 [.도움말] 명령어를 입력하세요.");
                }

                await Task.Delay(1000);
            }

            player.ShowHint("");
            player.ClearBroadcasts();
        }

        public async void OnRoundStarted()
        {
            player.Role.Set(RoleTypeId.FacilityGuard);
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);
            Map.TurnOffAllLights(99999);

            Player.Get(11).DisplayNickname = "Phone Guy";
            Player.Get(11).Kill("닉네임 동기화");

            Tasks.Instance = new Tasks();

            await Task.WhenAll
            (
                Tasks.Instance.PhoneGuy(),
                Tasks.Instance.Sync079andBattery(),
                Tasks.Instance.Timer(),
                Tasks.Instance.UsingBattery(),
                Tasks.Instance.ShowBattery(),

                Tasks.Instance.Scp049(SetLevel["SCP-049"]),
                Tasks.Instance.Scp939(SetLevel["SCP-939"]),
                Tasks.Instance.Scp0492(SetLevel["SCP-049-2"]),
                Tasks.Instance.Scp106(SetLevel["SCP-106"]),
                Tasks.Instance.Scp3114(SetLevel["SCP-3114"]),
                Tasks.Instance.Scp096(SetLevel["SCP-096"]),
                Tasks.Instance.Scp173(SetLevel["SCP-173"])
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

        public void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            if (ev.Player == player)
            {
                Player.Get(11).DisplayNickname = "Game Over";
                Player.Get(11).Group = new UserGroup { BadgeColor = "red" };
                Gtool.PlaySound("PhoneGuy", $"jumpscare-{Killer}", VoiceChatChannel.Proximity, 5000);
            }
        }

        public void OnFlippingCoin(Exiled.Events.EventArgs.Player.FlippingCoinEventArgs ev)
        {
            ReferenceHub playerHub = ev.Player.ReferenceHub;
            FpcStandardRoleBase playerRole = playerHub.roleManager.CurrentRole as FpcStandardRoleBase;
            Vector3 playerForward = playerRole.transform.forward;

            ServerConsole.AddLog($"{ev.Player.Nickname}의 위치 : new Vector3({ev.Player.Position.x}f, {ev.Player.Position.y}f, {ev.Player.Position.z}f)", ConsoleColor.DarkMagenta);
            ServerConsole.AddLog($"{ev.Player.Nickname}의 방향 : new Vector3({playerForward.x}f, {playerForward.y}f, {playerForward.z}f)", ConsoleColor.Blue);
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
                IsCCTV = true;
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
                if (ev.Door.Type == DoorType.Scp079Armory)
                {
                    if (ev.Door.IsOpen)
                        Using.Add("Scp079ArmoryClose");
                    else
                        Using.Remove("Scp079ArmoryClose");
                }
                else if (ev.Door.Type == DoorType.HeavyContainmentDoor)
                {
                    if (ev.Door.IsOpen)
                        Using.Add("HeavyContainmentDoorClose");
                    else
                        Using.Remove("HeavyContainmentDoorClose");
                }
            }
        }

        public void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            if (ev.Player.Role.Type == RoleTypeId.FacilityGuard)
                ev.Player.ClearInventory();
        }

        public void OnDroppingItem(Exiled.Events.EventArgs.Player.DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
            pd.TryOverridePosition(new Vector3(46.32286f, 0.91f, 64.23f), Vector3.zero);

            player.Role.Set(RoleTypeId.FacilityGuard);
            player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
            player.EnableEffect(EffectType.Ensnared);

            Using.Remove("CCTV");
            IsCCTV = false;
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

        public void OnChangingSpeakerStatus(Exiled.Events.EventArgs.Scp079.ChangingSpeakerStatusEventArgs ev)
        {
            if (IsFemur != true && ev.Scp079.Camera.Name == "106 RECONTAINMENT")
                IsFemur = true;
        }

        public void OnChangingCamera(Exiled.Events.EventArgs.Scp079.ChangingCameraEventArgs ev)
        {
            ev.AuxiliaryPowerCost = 0;
        }

        public void OnAddingTarget(Exiled.Events.EventArgs.Scp096.AddingTargetEventArgs ev)
        {
            IsLookedatScp096 = true;
        }
    }
}

