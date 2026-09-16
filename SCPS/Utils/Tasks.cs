using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SCPS.Locations;
using MultiBroadcast.API;
using Exiled.API.Features;
using Exiled.API.Enums;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using Exiled.API.Features.Roles;
using RelativePositioning;
using ProjectMER.Features;

namespace SCPS
{
    class Tasks
    {
        public static Tasks Instance;

        private bool IsActive => ReferenceEquals(Instance, this) &&
                                 SCPS.Instance != null &&
                                 !SCPS.Instance.IsEnd;

        private async Task<bool> DelayWhileActive(int milliseconds)
        {
            await Task.Delay(milliseconds);
            return IsActive;
        }

        public async Task Sync079andBattery()
        {
            while (IsActive)
            {
                foreach (var scp in Player.List.Where(x => x.Role.Type == RoleTypeId.Scp079))
                {
                    if (scp.Role is Scp079Role scp079)
                        scp079.Energy = SCPS.Instance.Battery;
                }

                await Task.Delay(100);
            }
        }

        public async Task ShowBattery()
        {
            while (IsActive)
            {
                string UsageBar = "";

                foreach (var _ in SCPS.Instance.Using)
                    UsageBar += "[]";


                string ColorTag()
                {
                    int count = SCPS.Instance.Using.Count;

                    if (count < 2)
                        return "40FF00";

                    else if (count < 3)
                        return "FFFF00";

                    else if (count < 4)
                        return "FFBF00";

                    else return "FF0000";
                }

                foreach (var p in Player.List)
                    p.ShowHint($"\n\n\n\n\n\n\n\n<align=left><size=25>Power Left : <i>{(int)SCPS.Instance.Battery}%</i>\nUsage : <color=#{ColorTag()}>{UsageBar}</color></size></align>", 1f);
                await Task.Delay(500);
            }
        }

        public async Task UsingBattery()
        {
            while (IsActive)
            {
                if (SCPS.Instance.Battery < 0.3f)
                {
                    foreach (var obj in MapUtils.LoadedMaps.Values.SelectMany(map => map.SpawnedObjects).ToArray())
                    {
                        if (obj.name == "CustomSchematic-rlight" || obj.name == "CustomSchematic-Button")
                            obj.Destroy();
                    }

                    Player s0p = Player.List.ToList().Find(x => x.Role.Type == RoleTypeId.Scp079);

                    if (s0p != null)
                    {
                        if (s0p.Role.Type == RoleTypeId.Scp079)
                        {
                            ReferenceHub pd = SCPS.Instance.Chracters.Find(x => x.Name == "PlayerDummy").npc;
                            Gtool.Place(pd, SCPS.Instance.Locations.Get("dummy.hidden"));

                            s0p.Role.Set(RoleTypeId.FacilityGuard);
                            Gtool.Place(s0p, SCPS.Instance.Locations.Get("guard.office"));
                        }

                        foreach (var door in Exiled.API.Features.Doors.BreakableDoor.List)
                        {
                            if (!door.IsLocked)
                                door.Lock(100, DoorLockType.Regular079);
                            door.IsOpen = true;
                            SCPS.Instance.Using.Clear();
                        }

                        s0p.EnableEffect(EffectType.Scanned);

                        break;
                    }
                }

                SCPS.Instance.Battery -= SCPS.Instance.Using.Count * 0.024f;
                await Task.Delay(100);
            }
        }

        public async Task Timer()
        {
            if (!await DelayWhileActive(1000))
                return;

            Player.List.ToList().ForEach(x => x.AddBroadcast(45, "<b><size=40>12AM</size></b>"));
            if (!await DelayWhileActive(45000))
                return;

            for (int t = 1; t < 6; t++)
            {
                Player.List.ToList().ForEach(x => x.AddBroadcast(45, $"<b><size=40>{t}AM</size></b>"));
                if (!await DelayWhileActive(35000))
                    return;

                if (t == 5)
                    Server.ExecuteCommand($"/server_event play_effect_mtf");

                if (!await DelayWhileActive(10000))
                    return;
            }

            if (SCPS.Instance.IsEnd)
                return;

            SCPS.Instance.CompleteActiveNight();
            Gtool.PlayGlobalSound("fnaf-end", 30);
            SCPS.Instance.IsEnd = true;

            foreach (Player player in Player.List)
                player.Kill("6시!!");

            Player.List.ToList().ForEach(x => x.ShowHint("<size=150><b>5AM</b></size>\n\n\n\n\n\n\n\n\n\n", 5));
            await Task.Delay(4000);
            Player.List.ToList().ForEach(x => x.ShowHint("<size=150><b>6AM</b></size>\n\n\n\n\n\n\n\n\n\n", 10));
            await Task.Delay(6000);
            Round.IsLocked = false;
        }

        public async Task Scp049(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp049.stage", 9);
            int Phase = 0;

            ReferenceHub scp049 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp049").npc;
            ReferenceHub scp049dummy = SCPS.Instance.Chracters.Find(x => x.Name == "Scp049Dummy").npc;
            Gtool.Place(scp049, Stage[0]);

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, Gtool.PlayerGet("Scp049").CurrentRoom != Gtool.CameraRoom() ? 26 : 41);

                    if (rn == 25)
                    {
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        else if (Phase == Stage.Count - 1)
                        {
                            if (SCPS.Instance.Using.Contains("Scp079ArmoryClose"))
                                Phase = UnityEngine.Random.Range(3, 5);

                            else
                            {
                                SCPS.Instance.Killer = "Scp049";
                                Player.List.ToList().ForEach(x => x.Kill("SCP-049가 당신의 심장 소리를 지웠습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                        if (UnityEngine.Random.Range(1, 5) == 1 || Phase == 1)
                        {
                            Gtool.PlayerGet("Scp049Dummy").DisplayNickname = Gtool.GetRandomValue(new List<object> { "I recognize your presence", "I am watching you", "where my treatment is needed", "SCP-049" }).ToString();

                            Gtool.PlaySound("Scp049Dummy", $"scp049-{UnityEngine.Random.Range(1, 10)}", Volume: 20);
                        }

                        Gtool.Place(scp049, Stage[Phase]);
                        Gtool.Place(scp049dummy, Stage[Phase]);

                        scp049dummy.authManager.UserId = "ID_Dedicated";
                        scp049dummy.authManager.NetworkSyncedUserId = "ID_Dedicated";
                        scp049dummy.characterClassManager.GodMode = true;
                        scp049dummy.transform.localScale = Vector3.one * -0.01f;
                        foreach (Player player in Player.List)
                        {
                            Server.SendSpawnMessage.Invoke(null, new object[]
                            {
                                scp049dummy.netIdentity,
                                player.Connection
                            });
                        }
                        FirstPersonMovementModule fpcModule = (scp049dummy.roleManager.CurrentRole as FpcStandardRoleBase).FpcModule;
                        fpcModule.Position = scp049.transform.position + Vector3.up * 0.65f;
                        fpcModule.Motor.ReceivedPosition = new RelativePosition(scp049.transform.position + Vector3.up * 0.65f);
                        fpcModule.Noclip.IsActive = true;
                    }
                } 
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp939(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp939.stage", 9);
            int Phase = 0;

            ReferenceHub scp939 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp939").npc;
            Gtool.Place(scp939, Stage[0]);

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, Gtool.PlayerGet("Scp939").CurrentRoom != Gtool.CameraRoom() ? 26 : 41);

                    if (rn == 25)
                    {
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        else if (Phase == Stage.Count - 1)
                        {
                            if (SCPS.Instance.Using.Contains("Scp079ArmoryClose"))
                                Phase = UnityEngine.Random.Range(1, 6);

                            else
                            {
                                SCPS.Instance.Killer = "Scp939";
                                Player.List.ToList().ForEach(x => x.Kill("SCP-939가 당신을 찢었습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                        Gtool.Place(scp939, Stage[Phase]);
                    }
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp0492(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp0492.stage", 9);
            int Phase = 0;

            Ragdoll scp0492 = null;

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    int rn = UnityEngine.Random.Range(level, 26);

                    if (rn == 25)
                    {
                        if (scp0492 != null)
                            scp0492.UnSpawn();

                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        else if (Phase == Stage.Count - 1)
                        {
                            if (SCPS.Instance.Using.Contains("HeavyContainmentDoorClose"))
                                Phase = 0;

                            else
                            {
                                SCPS.Instance.Killer = "Scp0492";
                                Player.List.ToList().ForEach(x => x.Kill("SCP-049-2가 당신을 먹어치웠습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                        scp0492 = Ragdoll.CreateAndSpawn(RoleTypeId.Scp0492, "SCP-049-2", "maybe here..", Stage[Phase].Position, Stage[Phase].Quaternion);
                    }
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp106(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp106.stage", 9);
            int Phase = 0;

            ReferenceHub scp106 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp106").npc;

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, Gtool.PlayerGet("Scp106").CurrentRoom != Gtool.CameraRoom() ? 35 : 60);

                    if (rn == 29)
                    {
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                            Gtool.Place(scp106, Stage[Phase]);

                        if (Phase == Stage.Count - 1)
                            {
                                if (!SCPS.Instance.IsFemur)
                                {
                                    float Countdown = 8 - (1 / 10 * level);

                                    while (Countdown > 0 && IsActive)
                                    {
                                        await Task.Delay(100);
                                        Countdown -= 0.1f;

                                        if (SCPS.Instance.IsFemur)
                                        {
                                            Gtool.PlayerGet("Scp106").DisplayNickname = "Femur Breaker";
                                            Gtool.PlaySound("Scp106", "femur", Volume: 20);
                                            await Task.Delay(8000);
                                            Gtool.PlayerGet("Scp106").Kill("비명 소리가 나는 곳으로..");
                                            return;
                                        }
                                    }
                                }

                            SCPS.Instance.Killer = "Scp106";
                            Player.List.ToList().ForEach(x => x.Kill("SCP-106가 당신을 초대했습니다."));

                            SCPS.Instance.IsEnd = true;
                            await Task.Delay(5000);
                            Round.IsLocked = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp3114(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp3114.stage", 9);
            int Phase = 0;

            Ragdoll scp3114ragdoll = null;
            ReferenceHub scp3114 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp3114").npc;

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    int rn = UnityEngine.Random.Range(level, 30);

                    if (rn == 25)
                    {
                        if (scp3114ragdoll != null)
                            scp3114ragdoll.UnSpawn();

                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        if (Phase == Stage.Count - 1)
                        {
                            Gtool.Place(scp3114, Stage[Phase]);

                            float Countdown = 3 - (1 / 10 * level);

                            bool Know = true;
                            while (Countdown > 0 && IsActive)
                            {
                                await Task.Delay(100);
                                Countdown -= 0.1f;

                                if (SCPS.Instance.IsCCTV)
                                {
                                    Phase = 0;
                                    Know = false;
                                    Gtool.Place(scp3114, SCPS.Instance.Locations.Get("scp3114.spawn"));
                                    break;
                                }
                            }
                            if (Know)
                            {
                                SCPS.Instance.Killer = "Scp3114";
                                Player.List.ToList().ForEach(x => x.Kill("SCP-3114가 당신이 인간이라는 것을 알아차렸습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }
                        else
                            scp3114ragdoll = Ragdoll.CreateAndSpawn(RoleTypeId.Scp3114, "SCP-3114", "It smells like a human..", Stage[Phase].Position, Stage[Phase].Quaternion);
                    }
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp096(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp096.stage", 13);
            int Phase = 0;

            ReferenceHub scp096 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp096").npc;
            Gtool.Place(scp096, Stage[0]);

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, 26);

                    if (rn == 25)
                    {
                        if (Gtool.PlayerGet("Scp096").CurrentRoom == Gtool.CameraRoom() && !SCPS.Instance.IsEnd)
                        {
                            SCPS.Instance.Killer = "Scp096";
                            Player.List.ToList().ForEach(x => x.Kill("SCP-096이 당신을 無로 되돌렸습니다."));

                            SCPS.Instance.IsEnd = true;
                            await Task.Delay(5000);
                            Round.IsLocked = false;
                        }

                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        Gtool.Place(scp096, Stage[Phase]);

                        if (Phase == Stage.Count - 1)
                            Phase = UnityEngine.Random.Range(1, 4);
                    }
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp173(int level)
        {
            List<ScpsLocation> Stage = SCPS.Instance.Locations.GetStages("scp173.stage", 5);
            int Phase = 0;

            ReferenceHub scp173 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp173").npc;
            Gtool.Place(scp173, Stage[0]);

            if (level < 1)
                return;

            while (IsActive)
            {
                try
                {
                    await Task.Delay(2000);

                    int rn = UnityEngine.Random.Range(level, Gtool.PlayerGet("Scp173").CurrentRoom != Gtool.CameraRoom() ? 26 : 41);

                    if (rn == 25)
                    {
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        if (Phase == Stage.Count - 1)
                        {
                            ScpsLocation run1 = SCPS.Instance.Locations.Get("scp173.run.01");
                            ScpsLocation run2 = SCPS.Instance.Locations.Get("scp173.run.02");
                            ScpsLocation run3 = SCPS.Instance.Locations.Get("scp173.run.03");
                            ScpsLocation run4 = SCPS.Instance.Locations.Get("scp173.run.04");
                            ScpsLocation run5 = SCPS.Instance.Locations.Get("scp173.run.05");
                            Gtool.Place(scp173, run1);
                            await MoveNpc(scp173, run1, run2, 37, 50);
                            await MoveNpc(scp173, run2, run3, 30, 50);
                            await MoveNpc(scp173, run3, run4, 12, 100);
                            await MoveNpc(scp173, run4, run5, 3, 150);

                            if (SCPS.Instance.Using.Contains("Scp079ArmoryClose"))
                                Phase = 0;

                            else
                            {
                                SCPS.Instance.Killer = "Scp173";
                                Player.List.ToList().ForEach(x => x.Kill("SCP-173이 당신의 목을 꺾었습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                        Gtool.Place(scp173, Stage[Phase]);
                    }
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        private static async Task MoveNpc(ReferenceHub npc, ScpsLocation from, ScpsLocation to, int steps, int delayMilliseconds)
        {
            int count = Math.Max(1, steps);
            for (int index = 1; index <= count; index++)
            {
                float progress = index / (float)count;
                npc.TryOverridePosition(Vector3.Lerp(from.Position, to.Position, progress));
                Gtool.Rotate(npc, Quaternion.Slerp(from.Quaternion, to.Quaternion, progress));
                await Task.Delay(delayMilliseconds);
            }
        }
    }
}
