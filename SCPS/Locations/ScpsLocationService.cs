using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Newtonsoft.Json;
using UnityEngine;

namespace SCPS.Locations
{
    public sealed class ScpsLocationService
    {
        private readonly Dictionary<string, ScpsLocation> locations =
            new Dictionary<string, ScpsLocation>(StringComparer.OrdinalIgnoreCase);

        public ScpsLocationService()
        {
            RootDirectory = Path.Combine(Paths.Configs, "GG", "SCPS");
            FilePath = Path.Combine(RootDirectory, "Map", "layout.json");
            Load();
        }

        public string RootDirectory { get; }

        public string FilePath { get; }

        public IReadOnlyList<ScpsLocation> All => locations.Values
            .OrderBy(location => location.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        public ScpsLocation Get(string key)
        {
            if (!locations.TryGetValue(key, out ScpsLocation location))
                throw new KeyNotFoundException($"Unknown SCPS location: {key}");
            return location;
        }

        public List<ScpsLocation> GetStages(string prefix, int count)
        {
            List<ScpsLocation> result = new List<ScpsLocation>(count);
            for (int index = 1; index <= count; index++)
                result.Add(Get($"{prefix}.{index:00}"));
            return result;
        }

        public void Capture(string key, Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            ScpsLocation location = Get(key);
            location.Position = player.Position;
            location.Rotation = player.Rotation.eulerAngles;
            location.Captured = true;
            Save();
        }

        private void Load()
        {
            foreach (ScpsLocation location in CreateDefaults())
                locations[location.Key] = location;

            try
            {
                if (File.Exists(FilePath))
                {
                    List<ScpsLocation> saved = JsonConvert.DeserializeObject<List<ScpsLocation>>(File.ReadAllText(FilePath));
                    foreach (ScpsLocation location in saved ?? new List<ScpsLocation>())
                    {
                        if (location == null || string.IsNullOrWhiteSpace(location.Key) || !locations.ContainsKey(location.Key))
                            continue;

                        ScpsLocation target = locations[location.Key];
                        target.Position = location.Position;
                        target.Rotation = location.Rotation;
                        target.Captured = location.Captured;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error($"[SCPS] Failed to load map locations: {exception}");
            }

            Save();
        }

        private void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                Directory.CreateDirectory(directory);
                if (File.Exists(FilePath))
                    File.Copy(FilePath, FilePath + ".backup", true);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(All, Formatting.Indented));
            }
            catch (Exception exception)
            {
                Log.Error($"[SCPS] Failed to save map locations: {exception}");
                throw;
            }
        }

        private static IEnumerable<ScpsLocation> CreateDefaults()
        {
            List<ScpsLocation> result = new List<ScpsLocation>();

            void Add(string key, string english, string korean, Vector3 position, Vector3 direction)
            {
                Quaternion rotation = direction.sqrMagnitude < 0.0001f ||
                                      Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.999f
                    ? Quaternion.identity
                    : Quaternion.LookRotation(direction, Vector3.up);
                result.Add(new ScpsLocation
                {
                    Key = key,
                    NameEnglish = english,
                    NameKorean = korean,
                    Position = position,
                    Rotation = rotation.eulerAngles,
                });
            }

            void Stage(string prefix, string english, string korean, int number, Vector3 position, Vector3 direction)
                => Add($"{prefix}.{number:00}", $"{english} stage {number}", $"{korean} {number}단계", position, direction);

            Add("guard.office", "Guard office", "경비원 사무실", new Vector3(68.2181f, -1002.403f, 54.75781f), new Vector3(1f, -2f, 2f));
            Add("dummy.hidden", "Player dummy storage", "플레이어 더미 보관 위치", new Vector3(46.32286f, 2.41f, 64.23f), Vector3.forward);
            Add("scp049.spawn", "SCP-049 initial spawn", "SCP-049 초기 소환", new Vector3(38.65023f, -805.1f, 81.84583f), new Vector3(-1f, -1f, 1f));
            Add("scp049.dummy", "SCP-049 victim dummy", "SCP-049 희생자 더미", new Vector3(38.65023f, -805.1f, 81.84583f), new Vector3(-1f, -1f, 1f));
            Add("scp939.spawn", "SCP-939 initial spawn", "SCP-939 초기 소환", new Vector3(98.94531f, -997.155f, 93.27344f), Vector3.right);
            Add("scp106.spawn", "SCP-106 initial spawn", "SCP-106 초기 소환", new Vector3(28.48828f, -997.2513f, 152.0195f), Vector3.forward);
            Add("scp3114.spawn", "SCP-3114 storage", "SCP-3114 보관 위치", new Vector3(59f, -1002.776f, 67.01563f), Vector3.forward);
            Add("scp096.spawn", "SCP-096 initial spawn", "SCP-096 초기 소환", new Vector3(90.01107f, -997.5436f, 133.1367f), Vector3.back);
            Add("scp173.spawn", "SCP-173 initial spawn", "SCP-173 초기 소환", new Vector3(46.17308f, -800.735f, 96.46692f), Vector3.back);

            Stage("scp049.stage", "SCP-049", "SCP-049", 1, new Vector3(38.65023f, -806.6f, 81.84583f), new Vector3(-1, -1, 1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 2, new Vector3(49.71606f, -806.6f, 86.9866f), new Vector3(-2, 0, 1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 3, new Vector3(40.44612f, -806.6f, 109.3256f), new Vector3(0, 0, -1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 4, new Vector3(40.21581f, -999.04f, 90.77383f), new Vector3(5, 0, -1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 5, new Vector3(75.74154f, -999.0399f, 89.30859f), new Vector3(0, 0, -1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 6, new Vector3(74.08465f, -999.04f, 68.04793f), new Vector3(0, 0, -1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 7, new Vector3(72.72982f, -1002.372f, 45.83594f), new Vector3(-1, 0, 1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 8, new Vector3(65.51498f, -1002.273f, 52.85938f), new Vector3(1, 0, 1));
            Stage("scp049.stage", "SCP-049", "SCP-049", 9, new Vector3(64.68642f, -1002.372f, 54.64335f), new Vector3(1, 0, 0));

            Stage("scp939.stage", "SCP-939", "SCP-939", 1, new Vector3(98.94531f, -998.655f, 93.27344f), new Vector3(1, 0, 0));
            Stage("scp939.stage", "SCP-939", "SCP-939", 2, new Vector3(102.6856f, -999.04f, 92.9023f), new Vector3(0, 1, 0));
            Stage("scp939.stage", "SCP-939", "SCP-939", 3, new Vector3(106.028f, -999.0436f, 73.66406f), new Vector3(0.7890916f, 0f, -0.6142756f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 4, new Vector3(92.48823f, -999.0452f, 74.99609f), new Vector3(-1f, 0f, -2.396107E-05f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 5, new Vector3(77.72775f, -999.04f, 75.35055f), new Vector3(-0.4367688f, 0f, -0.8995739f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 6, new Vector3(74.88365f, -1002.264f, 54.14531f), new Vector3(-0.05409516f, 0f, -0.9985359f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 7, new Vector3(62.30162f, -1002.372f, 45.80297f), new Vector3(0.6691485f, 0f, 0.7431287f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 8, new Vector3(62.30162f, -1002.372f, 51.73516f), new Vector3(0.8737565f, 0f, 0.4863637f));
            Stage("scp939.stage", "SCP-939", "SCP-939", 9, new Vector3(62.84068f, -1002.372f, 54.85125f), new Vector3(0.9996569f, 0f, -0.02619493f));

            Vector3 corpse = new Vector3(70.16341f, -1003f, 64.92969f);
            for (int index = 1; index <= 5; index++) Stage("scp0492.stage", "SCP-049-2", "SCP-049-2", index, corpse, Vector3.forward);
            Stage("scp0492.stage", "SCP-049-2", "SCP-049-2", 6, new Vector3(67.99935f, -1003, 65.19922f), Vector3.forward);
            Stage("scp0492.stage", "SCP-049-2", "SCP-049-2", 7, new Vector3(68.17271f, -1003, 62.91094f), Vector3.forward);
            Stage("scp0492.stage", "SCP-049-2", "SCP-049-2", 8, new Vector3(68.21178f, -1003, 61.27813f), Vector3.forward);
            Stage("scp0492.stage", "SCP-049-2", "SCP-049-2", 9, new Vector3(68.50475f, -1003, 58.08281f), Vector3.forward);

            Vector3[] p106 = { new Vector3(40.04688f,-998.1693f,140.5391f), new Vector3(29.98039f,-999.1128f,127.9737f), new Vector3(28.82031f,-999.0364f,104.2031f), new Vector3(30.3906f,-999.0403f,75.19531f), new Vector3(49.58594f,-999.0403f,74.89063f), new Vector3(63.52734f,-999.0403f,68.98438f), new Vector3(72.64843f,-999.0403f,75.52344f), new Vector3(73.71029f,-999.0436f,60.875f), new Vector3(68.17271f,-1002.372f,54.14922f) };
            Vector3[] d106 = { new Vector3(-.03144205f,0,.9995056f), new Vector3(.01394957f,0,-.9999027f), new Vector3(-.2621398f,0,-.96503f), new Vector3(.9998474f,0,-.01747239f), new Vector3(-.7265648f,0,.687098f), new Vector3(.9245636f,0,-.3810276f), new Vector3(.2957431f,0,-.9552677f), new Vector3(.9788742f,0,.2044639f), new Vector3(-.0401616f,0,.9991932f) };
            for (int index = 0; index < p106.Length; index++) Stage("scp106.stage", "SCP-106", "SCP-106", index + 1, p106[index], d106[index]);

            Vector3[] p3114 = { new Vector3(63.16881f,-1001.9423f,58.59989f), new Vector3(63.99303f,-1001.966f,58.59989f), new Vector3(65.61803f,-1001.966f,58.59989f), new Vector3(70.0786f,-1001.9736f,54.80938f), new Vector3(70.0786f,-1001.8759f,55.99688f), new Vector3(70.0786f,-1003.2f,58.21563f), new Vector3(70.0786f,-1003.2f,55.66484f), new Vector3(70.0786f,-1003.099f,54.89531f), new Vector3(68.10631f,-1002.372f,55.91484f) };
            for (int index = 0; index < p3114.Length; index++) Stage("scp3114.stage", "SCP-3114", "SCP-3114", index + 1, p3114[index], index == 8 ? new Vector3(.01049824f,0,-.9999449f) : Vector3.forward);

            Vector3[] p096 = { new Vector3(89.92904f,-999.0436f,132.6211f), new Vector3(90.1959f,-999.04f,120.0317f), new Vector3(118.8789f,-999.0403f,120.6172f), new Vector3(119.7773f,-999.0403f,94.25f), new Vector3(119.6445f,-999.0403f,68.75781f), new Vector3(105.3796f,-999.0436f,62.69141f), new Vector3(98.92573f,-999.0403f,74.83594f), new Vector3(85.85157f,-999.0482f,75.03125f), new Vector3(59.16797f,-999.0403f,80.40234f), new Vector3(30.30076f,-999.0403f,75.08984f), new Vector3(29.83984f,-999.0403f,111.1211f), new Vector3(49.30466f,-999.0403f,119.875f), new Vector3(71.47656f,-999.0482f,120.4219f) };
            Vector3[] d096 = { new Vector3(.006663424f,0,-.9999778f), new Vector3(1,0,-.00002408028f), new Vector3(.1512359f,0,-.9884979f), new Vector3(-.03311848f,0,-.9994514f), new Vector3(.2706414f,0,-.9626803f), new Vector3(-.08369686f,0,.9964913f), new Vector3(-.9989709f,0,-.04535747f), new Vector3(-.9997424f,0,.0226965f), new Vector3(-.8260864f,0,-.5635436f), new Vector3(-.02971719f,0,.9995583f), new Vector3(.1754025f,0,.9844968f), new Vector3(.9999862f,0,.005249202f), new Vector3(.9845094f,0,-.1753316f) };
            for (int index = 0; index < p096.Length; index++) Stage("scp096.stage", "SCP-096", "SCP-096", index + 1, p096[index], d096[index]);

            Vector3[] p173 = { new Vector3(46.17308f,-802.235f,96.46692f), new Vector3(47.79235f,-802.235f,98.01788f), new Vector3(49.6346f,-802.235f,99.67786f), new Vector3(52.60084f,-802.235f,99.86421f), new Vector3(39.68065f,-999.0432f,90.03945f) };
            Vector3[] d173 = { new Vector3(.006663424f,0,-.9999778f), new Vector3(-.8903908f,0,-.4551969f), new Vector3(-.9984583f,0,-.05550671f), new Vector3(-.0003355424f,0,-1), new Vector3(.999989f,0,.004674017f) };
            for (int index = 0; index < p173.Length; index++) Stage("scp173.stage", "SCP-173", "SCP-173", index + 1, p173[index], d173[index]);
            Add("scp173.run.01", "SCP-173 run waypoint 1", "SCP-173 돌진 지점 1", new Vector3(39f,-999.0432f,90.03945f), Vector3.right);
            Add("scp173.run.02", "SCP-173 run waypoint 2", "SCP-173 돌진 지점 2", new Vector3(75f,-999.0432f,90.03945f), Vector3.right);
            Add("scp173.run.03", "SCP-173 run waypoint 3", "SCP-173 돌진 지점 3", new Vector3(75.02279f,-999.0436f,61f), Vector3.back);
            Add("scp173.run.04", "SCP-173 run waypoint 4", "SCP-173 돌진 지점 4", new Vector3(64f,-1002.372f,47.85625f), Vector3.left);
            Add("scp173.run.05", "SCP-173 run waypoint 5", "SCP-173 돌진 지점 5", new Vector3(65f,-1002.372f,54.64531f), Vector3.right);

            return result;
        }
    }
}
