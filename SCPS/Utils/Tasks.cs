using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Exiled.API.Features;
using Exiled.API.Enums;
using PlayerRoles.FirstPersonControl;
using PlayerRoles;
using Mirror;
using MEC;
using Exiled.API.Features.Roles;
using SCPSLAudioApi.AudioCore;
using VoiceChat;
using RelativePositioning;

namespace SCPS
{
    class Tasks
    {
        public static Tasks Instance;

        public async Task PhoneGuy()
        {
            Gtool.ClearSound("PhoneGuy");
        }

        public async Task Sync079andBattery()
        {
            while (true)
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
            while (!SCPS.Instance.IsEnd)
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

                SCPS.Instance.player.ShowHint($"\n\n\n\n\n\n\n\n<align=left><size=25>Power Left : <i>{(int)SCPS.Instance.Battery}%</i>\nUsage : <color=#{ColorTag()}>{UsageBar}</color></size></align>", 1f);
                await Task.Delay(500);
            }
        }

        public async Task UsingBattery()
        {
            while (true)
            {
                if (SCPS.Instance.Battery < 0.3f)
                {
                    foreach (var obj in MapEditorReborn.API.API.SpawnedObjects)
                    {
                        if (obj.name == "CustomSchematic-rlight" || obj.name == "CustomSchematic-Button")
                            obj.Destroy();
                    }

                    if (SCPS.Instance.player.Role.Type == RoleTypeId.Scp079)
                    {
                        ReferenceHub pd = SCPS.Instance.Chracters.Find(x => x.Name == "PlayerDummy").npc;
                        pd.TryOverridePosition(new Vector3(46.32286f, 0.91f, 64.23f), Vector3.zero);

                        SCPS.Instance.player.Role.Set(RoleTypeId.FacilityGuard);
                        SCPS.Instance.player.Position = new Vector3(68.2181f, -1002.403f, 54.75781f);
                        SCPS.Instance.player.EnableEffect(EffectType.Ensnared);
                    }

                    foreach (var door in Exiled.API.Features.Doors.BreakableDoor.List)
                    {
                        door.ChangeLock(DoorLockType.Regular079);
                        door.IsOpen = true;
                        SCPS.Instance.Using.Clear();
                    }

                    SCPS.Instance.player.EnableEffect(EffectType.Scanned);

                    break;
                }

                SCPS.Instance.Battery -= SCPS.Instance.Using.Count * 0.024f;
                await Task.Delay(100);
            }
        }

        public async Task Timer()
        {
            await Task.Delay(1000);
            SCPS.Instance.player.Broadcast(45, "<b><size=40>12AM</size></b>");
            await Task.Delay(45000);

            for (int t = 1; t < 6; t++)
            {
                SCPS.Instance.player.Broadcast(45, $"<b><size=40>{t}AM</size></b>");
                await Task.Delay(35000);
                if (t == 5)
                    Server.ExecuteCommand($"/server_event play_effect_mtf");
                await Task.Delay(10000);
            }

            foreach (var p in Player.List)
            {
                if (p != SCPS.Instance.player)
                    p.Kill("6시!!");
            }

            if (!SCPS.Instance.IsEnd)
            {
                Gtool.PlayerGet("PhoneGuy").DisplayNickname = "Congratulations!";
                Gtool.PlaySound("PhoneGuy", $"fnaf-end", VoiceChatChannel.Intercom, 30);

                SCPS.Instance.IsEnd = true;
                SCPS.Instance.player.ShowHint("<size=150><b>5AM</b></size>\n\n\n\n\n\n\n\n\n\n", 5);
                await Task.Delay(4000);
                SCPS.Instance.player.ShowHint("<size=150><b>6AM</b></size>\n\n\n\n\n\n\n\n\n\n", 10);
                await Task.Delay(6000);
                Round.IsLocked = false;
            }
        }

        public async Task Scp049(int level)
        {

            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(38.65023f, -806.6f, 81.84583f), new Vector3(-1, -1, 1) },
                new List<Vector3>() { new Vector3(49.71606f, -806.6f, 86.9866f), new Vector3(-2, 0, 1) },
                new List<Vector3>() { new Vector3(40.44612f, -806.6f, 109.3256f), new Vector3(0, 0, -1) },
                new List<Vector3>() { new Vector3(40.21581f, -999.04f, 90.77383f), new Vector3(5, 0, -1) },
                new List<Vector3>() { new Vector3(75.74154f, -999.0399f, 89.30859f), new Vector3(0, 0, -1) },
                new List<Vector3>() { new Vector3(74.08465f, -999.04f, 68.04793f), new Vector3(0, 0, -1) },
                new List<Vector3>() { new Vector3(72.72982f, -1002.372f, 45.83594f), new Vector3(-1, 0, 1) },
                new List<Vector3>() { new Vector3(65.51498f, -1002.273f, 52.85938f), new Vector3(1, 0, 1) },
                new List<Vector3>() { new Vector3(64.68642f, -1002.372f, 54.64335f), new Vector3(1, 0, 0) }
            };
            int Phase = 0;

            ReferenceHub scp049 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp049").npc;
            ReferenceHub scp049dummy = SCPS.Instance.Chracters.Find(x => x.Name == "Scp049Dummy").npc;
            scp049.TryOverridePosition(Stage[0][0], Vector3.zero);
            Gtool.Rotate(scp049, Stage[0][1]);

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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
                            Player.Get(7).DisplayNickname = Gtool.GetRandomValue(new List<object> { "I recognize your presence", "I am watching you", "where my treatment is needed", "SCP-049" }).ToString();

                            Gtool.PlaySound("Scp049Dummy", $"scp049-{UnityEngine.Random.Range(1, 10)}", Volume: 20);
                        }

                        scp049.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                        scp049dummy.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                        Gtool.Rotate(scp049, Stage[Phase][1]);

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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(98.94531f, -998.655f, 93.27344f), new Vector3(1, 0, 0) },
                new List<Vector3>() { new Vector3(102.6856f, -999.04f, 92.9023f), new Vector3(0, 1, 0) },
                new List<Vector3>() { new Vector3(106.028f, -999.0436f, 73.66406f), new Vector3(0.7890916f, 0f, -0.6142756f) },
                new List<Vector3>() { new Vector3(92.48823f, -999.0452f, 74.99609f), new Vector3(-1f, 0f, -2.396107E-05f) },
                new List<Vector3>() { new Vector3(77.72775f, -999.04f, 75.35055f), new Vector3(-0.4367688f, 0f, -0.8995739f) },
                new List<Vector3>() { new Vector3(74.88365f, -1002.264f, 54.14531f), new Vector3(-0.05409516f, 0f, -0.9985359f) },
                new List<Vector3>() { new Vector3(62.30162f, -1002.372f, 45.80297f), new Vector3(0.6691485f, 0f, 0.7431287f) },
                new List<Vector3>() { new Vector3(62.30162f, -1002.372f, 51.73516f), new Vector3(0.8737565f, 0f, 0.4863637f) },
                new List<Vector3>() { new Vector3(62.84068f, -1002.372f, 54.85125f), new Vector3(0.9996569f, 0f, -0.02619493f) },
            };
            int Phase = 0;

            ReferenceHub scp939 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp939").npc;
            scp939.TryOverridePosition(Stage[0][0], Vector3.zero);
            Gtool.Rotate(scp939, Stage[0][1]);

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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

                        scp939.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                        Gtool.Rotate(scp939, Stage[Phase][1]);
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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(70.16341f, -1003, 64.92969f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.16341f, -1003, 64.92969f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.16341f, -1003, 64.92969f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.16341f, -1003, 64.92969f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.16341f, -1003, 64.92969f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(67.99935f, -1003, 65.19922f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(68.17271f, -1003, 62.91094f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(68.21178f, -1003, 61.27813f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(68.50475f, -1003, 58.08281f), new Vector3(0, 0, 0) },
            };
            int Phase = 0;

            Ragdoll scp0492 = null;

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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

                        scp0492 = Ragdoll.CreateAndSpawn(RoleTypeId.Scp0492, "SCP-049-2", "maybe here..", Stage[Phase][0], new Quaternion(0, 0, 0, 0));
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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(40.04688f, -998.1693f, 140.5391f), new Vector3(-0.03144205f, 0f, 0.9995056f) },
                new List<Vector3>() { new Vector3(29.98039f, -999.1128f, 127.9737f), new Vector3(0.01394957f, 0f, -0.9999027f) },
                new List<Vector3>() { new Vector3(28.82031f, -999.0364f, 104.2031f), new Vector3(-0.2621398f, 0f, -0.96503f) },
                new List<Vector3>() { new Vector3(30.3906f, -999.0403f, 75.19531f), new Vector3(0.9998474f, 0f, -0.01747239f) },
                new List<Vector3>() { new Vector3(49.58594f, -999.0403f, 74.89063f), new Vector3(-0.7265648f, 0f, 0.687098f) },
                new List<Vector3>() { new Vector3(63.52734f, -999.0403f, 68.98438f), new Vector3(0.9245636f, 0f, -0.3810276f) },
                new List<Vector3>() { new Vector3(72.64843f, -999.0403f, 75.52344f), new Vector3(0.2957431f, 0f, -0.9552677f) },
                new List<Vector3>() { new Vector3(73.71029f, -999.0436f, 60.875f), new Vector3(0.9788742f, 0f, 0.2044639f) },
                new List<Vector3>() { new Vector3(68.17271f, -1002.372f, 54.14922f), new Vector3(-0.0401616f, 0f, 0.9991932f) },
            };
            int Phase = 0;

            ReferenceHub scp106 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp106").npc;

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, Gtool.PlayerGet("Scp106").CurrentRoom != Gtool.CameraRoom() ? 35 : 60);

                    if (rn == 29)
                    {
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                            scp106.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                            Gtool.Rotate(scp106, Stage[Phase][1]);

                        if (Phase == Stage.Count - 1)
                            {
                                if (!SCPS.Instance.IsFemur)
                                {
                                    float Countdown = 8 - (1 / 10 * level);

                                    while (Countdown > 0)
                                    {
                                        await Task.Delay(100);
                                        Countdown -= 0.1f;

                                        if (SCPS.Instance.IsFemur)
                                        {
                                            Player.Get(13).DisplayNickname = "Femur Breaker";
                                            Gtool.PlaySound("Scp106", "femur", Volume: 20);
                                            await Task.Delay(8000);
                                            Player.Get(13).Kill("비명 소리가 나는 곳으로..");
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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(63.16881f, -1001.9423f, 58.59989f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(63.99303f, -1001.966f, 58.59989f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(65.61803f, -1001.966f, 58.59989f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.0786f, -1001.9736f, 54.80938f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.0786f, -1001.8759f, 55.99688f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.0786f, -1003.2f, 58.21563f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.0786f, -1003.2f, 55.66484f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(70.0786f, -1003.099f, 54.89531f), new Vector3(0, 0, 0) },
                new List<Vector3>() { new Vector3(68.10631f, -1002.372f, 55.91484f), new Vector3(0.01049824f, 0f, -0.9999449f) },
            };
            int Phase = 0;

            Ragdoll scp3114ragdoll = null;
            ReferenceHub scp3114 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp3114").npc;

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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
                            scp3114.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                            Gtool.Rotate(scp3114, Stage[Phase][1]);

                            float Countdown = 3 - (1 / 10 * level);

                            bool Know = true;
                            while (Countdown > 0)
                            {
                                await Task.Delay(100);
                                Countdown -= 0.1f;

                                if (SCPS.Instance.IsCCTV)
                                {
                                    Phase = 0;
                                    Know = false;
                                    scp3114.TryOverridePosition(new Vector3(59f, -1004.276f, 67.01563f), Vector3.zero);
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
                            scp3114ragdoll = Ragdoll.CreateAndSpawn(RoleTypeId.Scp3114, "SCP-3114", "It smells like a human..", Stage[Phase][0], new Quaternion(0, 0, 0, 0));
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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(89.92904f, -999.0436f, 132.6211f), new Vector3(0.006663424f, 0f, -0.9999778f) },
                new List<Vector3>() { new Vector3(90.1959f, -999.04f, 120.0317f), new Vector3(1f, 0f, -2.408028E-05f) },
                new List<Vector3>() { new Vector3(118.8789f, -999.0403f, 120.6172f), new Vector3(0.1512359f, 0f, -0.9884979f) },
                new List<Vector3>() { new Vector3(119.7773f, -999.0403f, 94.25f), new Vector3(-0.03311848f, 0f, -0.9994514f) },
                new List<Vector3>() { new Vector3(119.6445f, -999.0403f, 68.75781f), new Vector3(0.2706414f, 0f, -0.9626803f) },
                new List<Vector3>() { new Vector3(105.3796f, -999.0436f, 62.69141f), new Vector3(-0.08369686f, 0f, 0.9964913f) },
                new List<Vector3>() { new Vector3(98.92573f, -999.0403f, 74.83594f), new Vector3(-0.9989709f, 0f, -0.04535747f) },
                new List<Vector3>() { new Vector3(85.85157f, -999.0482f, 75.03125f), new Vector3(-0.9997424f, 0f, 0.0226965f) },
                new List<Vector3>() { new Vector3(59.16797f, -999.0403f, 80.40234f), new Vector3(-0.8260864f, 0f, -0.5635436f) },
                new List<Vector3>() { new Vector3(30.30076f, -999.0403f, 75.08984f), new Vector3(-0.02971719f, 0f, 0.9995583f) },
                new List<Vector3>() { new Vector3(29.83984f, -999.0403f, 111.1211f), new Vector3(0.1754025f, 0f, 0.9844968f) },
                new List<Vector3>() { new Vector3(49.30466f, -999.0403f, 119.875f), new Vector3(0.9999862f, 0f, 0.005249202f) },
                new List<Vector3>() { new Vector3(71.47656f, -999.0482f, 120.4219f), new Vector3(0.9845094f, 0f, -0.1753316f) },
            };
            int Phase = 0;

            ReferenceHub scp096 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp096").npc;
            scp096.TryOverridePosition(Stage[0][0], Vector3.zero);
            Gtool.Rotate(scp096, Stage[0][1]);

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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

                        scp096.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                        Gtool.Rotate(scp096, Stage[Phase][1]);

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
            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(46.17308f, -802.235f, 96.46692f), new Vector3(0.006663424f, 0f, -0.9999778f) },
                new List<Vector3>() { new Vector3(47.79235f, -802.235f, 98.01788f), new Vector3(-0.8903908f, 0f, -0.4551969f) },
                new List<Vector3>() { new Vector3(49.6346f, -802.235f, 99.67786f), new Vector3(-0.9984583f, 0f, -0.05550671f) },
                new List<Vector3>() { new Vector3(52.60084f, -802.235f, 99.86421f), new Vector3(-0.0003355424f, 0f, -1f) },
                new List<Vector3>() { new Vector3(39.68065f, -999.0432f, 90.03945f), new Vector3(0.999989f, 0f, 0.004674017f) }
            };
            int Phase = 0;

            ReferenceHub scp173 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp173").npc;
            scp173.TryOverridePosition(Stage[0][0], Vector3.zero);
            Gtool.Rotate(scp173, Stage[0][1]);

            if (level < 1)
                return;

            while (!SCPS.Instance.IsEnd)
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
                            for (int i=39; i<76; i++)
                            {
                                scp173.TryOverridePosition(new Vector3(i, -999.0432f, 90.03945f), Vector3.zero);
                                await Task.Delay(50);
                            }

                            for (int i=90; i>60; i--)
                            {
                                scp173.TryOverridePosition(new Vector3(75.02279f, -999.0436f, i), Vector3.zero);
                                await Task.Delay(50);
                            }

                            for (int i=75; i>63; i--)
                            {
                                scp173.TryOverridePosition(new Vector3(i, -1002.372f, 47.85625f), Vector3.zero);
                                await Task.Delay(100);
                            }

                            for (int i=63; i<66; i++)
                            {
                                scp173.TryOverridePosition(new Vector3(i, -1002.372f, 54.64531f), Vector3.zero);
                                await Task.Delay(150);
                            }

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

                        scp173.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                        Gtool.Rotate(scp173, Stage[Phase][1]);
                    }
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }
    }
}
