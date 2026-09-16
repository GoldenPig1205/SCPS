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
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using CustomPlayerEffects;
using GG.Core.Panel;
using GG.Core.Effects;
using SCPS.Locations;
using SCPS.Panel;
using SCPS.Progress;
using MultiBroadcast.API;

namespace SCPS
{
    public class SCPS : Plugin<Config>
    {
        private const int FixedMapSeed = 1205;

        public static SCPS Instance;

        public ScpsLocationService Locations { get; private set; }
        public ScpsProgressStore Progress { get; private set; }
        public int ActiveNight { get; private set; }
        public bool IsCustomNight { get; private set; }

        private readonly object roundStartLock = new object();
        private readonly HashSet<string> clearEligiblePlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<Chracters> Chracters = new List<Chracters>();

        public bool sync = false;
        public bool IsSetLevel = false;
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
            { "SCP-049", "난이도 : ★☆☆☆☆\n역병 의사라 불리는 그는 좌측 문에서 공격해올 것입니다. 가끔씩 혼잣말을 하거나 구두 소리를 냅니다." }, 
            { "SCP-939", "난이도 : ★★☆☆☆\n소리 없는 암살자입니다. 좌측 문에서 대기할 때 숨소리가 들립니다." }, 
            { "SCP-049-2", "난이도 : ★★☆☆☆\n앞쪽 환풍구를 통해서 당신에게 서서히 도달할 것입니다. 쓰러진 척 하는 연기가 속지 마십시오." }, 
            { "SCP-106", "난이도 : ★★★☆☆\n천천히 당신을 향해서 접근할 것입니다. 그는 시설 벽을 뚫고 당신에게 도달할 수 있습니다. 그를 막을 유일한 방법은, 그가 당신의 사무실에서 당신을 관찰하고 있을 때, 재빨리 CCTV를 SCP-106의 격리실로 옮긴 후, 스피커(v키)를 활성화하십시오." },
            { "SCP-3114", "난이도 : ★★☆☆☆\n당신의 채취를 쫒아 오른쪽 환풍구로 도달하기 위해 이동할 때마다 괴성을 내질러 당신의 귀를 방해할 것입니다. 그가 사무실에 나타났을 때 CCTV를 쳐다봄으로써 그를 속일 수 있습니다." },
            { "SCP-096", "난이도 : ★★★★☆\n그는 당신이 그의 얼굴을 \"확인\"하기 전까지는 절대로 해치지 않습니다. CCTV로 그를 발견했을 경우 최대한 빠르게 우회하십시오." },
            { "SCP-173", "난이도 : ★★★★★\n매우 재빠른 이 개체는 당신의 사무실로 돌진까지 4단계의 준비 과정이 있습니다. 그가 자신의 방을 떠난 경우 최대한 빠르게 문을 닫으십시오." }
        };
        public List<string> Using = new List<string>() { "RedLightOnSR" };

        public override void OnEnabled()
        {
            Instance = this;
            Locations = new ScpsLocationService();
            Progress = new ScpsProgressStore(Locations.RootDirectory);
            Progress.Initialize();
            PanelPackageRegistry.Register(new ScpsPanelPackage(Config, Locations, Progress, this).Create());

            Exiled.Events.Handlers.Map.Generating += OnMapGenerating;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Left += OnLeft;
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

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            IsEnd = true;
            Tasks.Instance = null;
            PanelPackageRegistry.Unregister(ScpsPanelPackage.Id);

            Exiled.Events.Handlers.Map.Generating -= OnMapGenerating;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
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

            Gtool.ClearAllSounds();
            Chracters.Clear();
            Progress?.SaveAll();
            Progress = null;
            Locations = null;
            Instance = null;
            base.OnDisabled();
        }

        public void OnMapGenerating(Exiled.Events.EventArgs.Map.GeneratingEventArgs ev)
        {
            ev.Seed = FixedMapSeed;
            Map.Seed = FixedMapSeed;
        }

        public async void OnWaitingForPlayers()
        {
            ResetRoundState();
            Map.CleanAllItems();

            while (Player.List.Count < 1)
                await Task.Delay(1000);

            Round.IsLobbyLocked = true;
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

            bool broadcast = false;

            while (Round.IsLobby)
            {
                if (Player.List.Count > 0)
                {
                    if (!broadcast)
                    {
                        foreach (var p in Player.List)
                            p.AddBroadcast(300, "<size=20><b>당신은 <color=red>SCP-079</color>의 전력을 제한하고 게이트를 여는 등 희생을 자처했지만, 모두가 탈출한 <color=#BDBDBD>Site-02</color> 기지에 홀로 버려졌습니다.\n" +
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
                    foreach (var p in Player.List)
                    {
                        if (!p.IsNPC)
                        {
                            if (EffectDisplayModule.IsPanelOpen(p))
                                p.ShowHint(string.Empty, 1);
                            else
                                p.ShowHint($"<align=left><b><size=50>A.I. Level</size></b>\n{output}</align>\n\n<color=#FACC2E>L키로 GG 패널을 열어 플레이할 밤을 선택하세요.</color>", 10);
                        }
                    }
                }

                await Task.Delay(1000);
            }

            foreach (var p in Player.List)
            {
                if (!p.IsNPC)
                {
                    p.ShowHint("");
                    p.ClearBroadcasts();
                }
            }
        }

        public async void OnRoundStarted()
        {
            clearEligiblePlayers.Clear();
            if (!IsCustomNight && ActiveNight >= 1 && ActiveNight <= 6)
            {
                foreach (Player player in Player.List.Where(player => !player.IsNPC))
                {
                    ScpsPlayerProgress profile = Progress.Get(player);
                    if (profile.IsNightUnlocked(ActiveNight) && profile.HighestClearedNight < ActiveNight)
                        clearEligiblePlayers.Add(player.UserId);
                }
            }

            foreach (var p in Player.List)
            {
                if (!p.IsNPC)
                {
                    p.Role.Set(RoleTypeId.FacilityGuard);
                    Gtool.Place(p, Locations.Get("guard.office"));
                }
            }
            Map.TurnOffAllLights(99999);

            Tasks.Instance = new Tasks();

            await Task.WhenAll
            (
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
            IsEnd = true;
            Tasks.Instance = null;
            Gtool.ClearAllSounds();
            Server.ExecuteCommand("sr");
        }

        private void ResetRoundState()
        {
            Tasks.Instance = null;
            Gtool.ClearAllSounds();
            Chracters.Clear();

            sync = false;
            IsSetLevel = false;
            IsEnd = false;
            IsFemur = false;
            IsLookedatScp096 = false;
            IsCCTV = false;
            Battery = 100f;
            Killer = null;
            ActiveNight = 0;
            IsCustomNight = false;
            clearEligiblePlayers.Clear();

            Using.Clear();
            Using.Add("RedLightOnSR");

            foreach (string key in SetLevel.Keys.ToArray())
                SetLevel[key] = 0;
        }

        public void OnVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            try
            {
                Progress.LoadOrCreate(ev.Player.UserId, ev.Player.Nickname);
            }
            catch (Exception exception)
            {
                Log.Error($"[SCPS] Failed to load progress for {ev.Player.UserId}: {exception}");
            }

            if (!Round.IsLobby)
            {
                ev.Player.Role.Set(RoleTypeId.FacilityGuard);
                Gtool.Place(ev.Player, Locations.Get("guard.office"));
            }
            else
            {
                if (Player.List.Where(x => !x.IsNPC).ToList().Count == 1)
                {
                    ReferenceHub PlayerDummy = Gtool.Spawn(RoleTypeId.FacilityGuard, Locations.Get("dummy.hidden"));
                    ReferenceHub Scp049 = Gtool.Spawn(RoleTypeId.Scp049, Locations.Get("scp049.spawn"));
                    ReferenceHub Scp049Dummy = Gtool.Spawn(RoleTypeId.ClassD, Locations.Get("scp049.dummy"));
                    ReferenceHub Scp939 = Gtool.Spawn(RoleTypeId.Scp939, Locations.Get("scp939.spawn"));
                    ReferenceHub Scp106 = Gtool.Spawn(RoleTypeId.Scp106, Locations.Get("scp106.spawn"));
                    ReferenceHub Scp3114 = Gtool.Spawn(RoleTypeId.Scp3114, Locations.Get("scp3114.spawn"));
                    ReferenceHub Scp096 = Gtool.Spawn(RoleTypeId.Scp096, Locations.Get("scp096.spawn"));
                    ReferenceHub Scp173 = Gtool.Spawn(RoleTypeId.Scp173, Locations.Get("scp173.spawn"));

                    Dictionary<ReferenceHub, string> register = new Dictionary<ReferenceHub, string>()
                    {
                        { PlayerDummy, "PlayerDummy" }, { Scp049, "Scp049" }, { Scp049Dummy, "Scp049Dummy" }, { Scp939, "Scp939" },
                        { Scp106, "Scp106" }, { Scp3114, "Scp3114" }, { Scp096, "Scp096" }, { Scp173, "Scp173" }
                    };

                    foreach (var reg in register)
                        Gtool.Register(reg.Key, reg.Value);

                    Gtool.PlayGlobalSound($"bgm-{UnityEngine.Random.Range(1, 8)}", 30, loop: true);
                }
            }
        }

        public void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            Progress?.SaveAndUnload(ev.Player?.UserId);
            if (Player.List.Count < 1)
                Server.ExecuteCommand("sr");
        }

        public bool TryStartNight(Player requester, int night, IReadOnlyDictionary<string, int> customLevels, out string errorCode)
        {
            errorCode = string.Empty;
            if (requester == null || requester.IsNPC || string.IsNullOrWhiteSpace(requester.UserId))
            {
                errorCode = "invalid-player";
                return false;
            }

            lock (roundStartLock)
            {
                if (!Round.IsLobby || Round.IsStarted || IsSetLevel)
                {
                    errorCode = "already-starting";
                    return false;
                }

                ScpsPlayerProgress profile = Progress.Get(requester);
                bool custom = night == 0;
                if (custom)
                {
                    if (!profile.IsCustomUnlocked)
                    {
                        errorCode = "custom-locked";
                        return false;
                    }

                    if (customLevels == null)
                    {
                        errorCode = "missing-levels";
                        return false;
                    }

                    foreach (string key in SetLevel.Keys.ToArray())
                    {
                        int value = customLevels.TryGetValue(key, out int level) ? level : 1;
                        SetLevel[key] = Math.Max(1, Math.Min(20, value));
                    }
                }
                else
                {
                    if (!profile.IsNightUnlocked(night))
                    {
                        errorCode = "night-locked";
                        return false;
                    }

                    ScpsNightPreset preset = ScpsNightPreset.Find(Config.NightPresets, night);
                    if (preset == null)
                    {
                        errorCode = "preset-missing";
                        return false;
                    }

                    foreach (string key in SetLevel.Keys.ToArray())
                        SetLevel[key] = preset.GetLevel(key);
                }

                ActiveNight = custom ? 0 : night;
                IsCustomNight = custom;
                IsSetLevel = true;
                try
                {
                    Server.ExecuteCommand("/mp load SCPS");
                    Round.Start();
                    return true;
                }
                catch (Exception exception)
                {
                    Log.Error($"[SCPS] Failed to load the SCPS map or start the round: {exception}");
                    ActiveNight = 0;
                    IsCustomNight = false;
                    IsSetLevel = false;
                    foreach (string key in SetLevel.Keys.ToArray())
                        SetLevel[key] = 0;
                    errorCode = "start-failed";
                    return false;
                }
            }
        }

        public void CompleteActiveNight()
        {
            if (IsCustomNight || ActiveNight < 1 || ActiveNight > 6 || Progress == null)
                return;

            foreach (Player player in Player.List.Where(player => !player.IsNPC && player.IsAlive).ToArray())
            {
                if (!clearEligiblePlayers.Contains(player.UserId))
                    continue;

                try
                {
                    if (Progress.MarkNightCleared(player, ActiveNight))
                    {
                        player.AddBroadcast(10, $"<size=30><color=#80F537><b>{ActiveNight}일밤 클리어!</b></color></size>");
                        Log.Info($"[SCPS] {player.Nickname} ({player.UserId}) cleared Night {ActiveNight}.");
                    }
                }
                catch (Exception exception)
                {
                    Log.Error($"[SCPS] Failed to save Night {ActiveNight} clear for {player.UserId}: {exception}");
                }
            }

            clearEligiblePlayers.Clear();
        }

        public void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            if (!ev.Player.IsNPC && !string.IsNullOrWhiteSpace(Killer))
            {
                Gtool.PlayGlobalSound($"jumpscare-{Killer}", 5000);
            }
        }

        public void OnActivatingWorkstation(Exiled.Events.EventArgs.Player.ActivatingWorkstationEventArgs ev)
        {
            ev.IsAllowed = false;

            if (!IsCCTV && !(Battery < 0.3f))
            {
                IsCCTV = true;

                ev.Player.Role.Set(RoleTypeId.Scp079);

                ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
                Gtool.Place(pd, Locations.Get("guard.office"));
                Player.Get(pd.PlayerId).CustomName = ev.Player.DisplayNickname;

                Using.Add("CCTV");

                if (ev.Player.Role is Scp079Role scp079)
                    scp079.AddExperience(1205);
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
            {
                ev.Player.ClearInventory();
                if (!ev.Player.IsNPC)
                    Gtool.Place(ev.Player, Locations.Get("guard.office"));
            }
        }

        public void OnDroppingItem(Exiled.Events.EventArgs.Player.DroppingItemEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
        {
            ReferenceHub pd = Chracters.Find(x => x.Name == "PlayerDummy").npc;
            Gtool.Place(pd, Locations.Get("dummy.hidden"));

            ev.Player.Role.Set(RoleTypeId.FacilityGuard);
            Gtool.Place(ev.Player, Locations.Get("guard.office"));

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

