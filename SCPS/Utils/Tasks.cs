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
using MEC;

namespace SCPS
{
    class Tasks
    {
        public static Tasks Instance;

        public async Task Sync079andBattery()
        {
            while (true)
            {
                foreach (var scp in Player.List.Where(x => x.Role.Type == RoleTypeId.Scp079))
                {
                    if (scp.Role is Exiled.API.Features.Roles.Scp079Role scp079)
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
                    UsageBar += "▮";

                SCPS.Instance.player.ShowHint($"\n\n\n\n\n\n\n\n<align=left><size=25>Power Left : {(int)SCPS.Instance.Battery}%\nUsage : {UsageBar}</size></align>", 1f);
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

                SCPS.Instance.Battery -= SCPS.Instance.Using.Count * 0.0215f;
                await Task.Delay(100);
            }
        }

        public async Task Timer()
        {
            SCPS.Instance.player.Broadcast(45, "<b><size=40>12AM</size></b>");
            await Task.Delay(45000);

            for (int t = 1; t < 6; t++)
            {
                SCPS.Instance.player.Broadcast(45, $"<b><size=40>{t}AM</size></b>");
                await Task.Delay(45000);
            }

            SCPS.Instance.IsEnd = true;
            SCPS.Instance.player.ShowHint("<size=150><b>5AM</b></size>\n\n\n\n\n\n\n\n\n\n", 5);
            await Task.Delay(3000);
            SCPS.Instance.player.ShowHint("<size=150><b>6AM</b></size>\n\n\n\n\n\n\n\n\n\n", 10);
            await Task.Delay(5000);
            Round.IsLocked = false;
        }

        public async Task Scp049(int level)
        {
            if (level < 1)
                return;

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

            while (!SCPS.Instance.IsEnd)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, 26);

                    if (rn == 25)
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        else if (Phase == Stage.Count - 1)
                        {
                            if (SCPS.Instance.Using.Contains("DoorClose"))
                                Phase = UnityEngine.Random.Range(3, 5);

                            else
                            {
                                Player.List.ToList().ForEach(x => x.Kill("SCP-049가 당신의 심장 소리를 지웠습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                    scp049.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                    Gtool.Rotate(scp049, Stage[Phase][1]);;
                } catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }

        public async Task Scp939(int level)
        {
            if (level < 1)
                return;

            List<List<Vector3>> Stage = new List<List<Vector3>>()
            {
                new List<Vector3>() { new Vector3(98.94531f, -998.655f, 93.27344f), new Vector3(1, 0, 0) }
            };
            int Phase = 0;

            ReferenceHub scp939 = SCPS.Instance.Chracters.Find(x => x.Name == "Scp939").npc;

            while (!SCPS.Instance.IsEnd)
            {
                try
                {
                    await Task.Delay(1000);

                    int rn = UnityEngine.Random.Range(level, 26);

                    /*
                    if (rn == 25)
                        if (Phase < (Stage.Count - 1))
                            Phase += 1;

                        else if (Phase == Stage.Count - 1)
                        {
                            if (SCPS.Instance.Using.Contains("DoorClose"))
                                Phase = UnityEngine.Random.Range(3, 5);

                            else
                            {
                                Player.List.ToList().ForEach(x => x.Kill("SCP-939가 당신을 찢었습니다."));

                                SCPS.Instance.IsEnd = true;
                                await Task.Delay(5000);
                                Round.IsLocked = false;
                            }
                        }

                    */
                    scp939.TryOverridePosition(Stage[Phase][0], Vector3.zero);
                    Gtool.Rotate(scp939, Stage[Phase][1]);
                }
                catch (Exception ex)
                {
                    ServerConsole.AddLog(ex.ToString());
                }
            }
        }
    }
}
