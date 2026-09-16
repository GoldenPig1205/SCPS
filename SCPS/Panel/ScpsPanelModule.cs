using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using GG.Core.Audio;
using GG.Core.Effects;
using GG.Core.Modules;
using GG.Core.Panel;
using GG.Core.Settings;
using SCPS.Locations;
using SCPS.Progress;

namespace SCPS.Panel
{
    public sealed class ScpsPanelModule : IServerModule
    {
        private const int VisibleLocationCount = 8;
        private const int CustomStartIndex = 7;
        private const int CustomBackIndex = 8;

        private static readonly string[] ScpNames =
        {
            "SCP-049", "SCP-939", "SCP-049-2", "SCP-106", "SCP-3114", "SCP-096", "SCP-173",
        };

        private readonly Config config;
        private readonly ScpsLocationService locations;
        private readonly ScpsProgressStore progress;
        private readonly SCPS plugin;
        private readonly PanelFrameRenderer renderer = new PanelFrameRenderer("GG.SCPS.Panel");
        private readonly HashSet<string> openPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> customViewPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> locationViewPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> mainIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> customIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> locationIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, int>> customLevels = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> notices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ScpsPanelModule(Config config, ScpsLocationService locations, ScpsProgressStore progress, SCPS plugin)
        {
            this.config = config;
            this.locations = locations;
            this.progress = progress;
            this.plugin = plugin;
        }

        public string Name => "SCPS GG panel";

        public void Enable()
        {
            GGInputRouter.InputReceived += OnInput;
            CommonSettingsModule.LanguageChanged += OnLanguageChanged;
            PanelNavigation.PanelChanged += OnPanelChanged;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Left += OnLeft;
        }

        public void Disable()
        {
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            PanelNavigation.PanelChanged -= OnPanelChanged;
            CommonSettingsModule.LanguageChanged -= OnLanguageChanged;
            GGInputRouter.InputReceived -= OnInput;
            foreach (Player player in Player.List.ToArray())
                Close(player);
            ClearSessions();
        }

        private void OnInput(Player player, GGInputAction action)
        {
            if (player == null || player.IsNPC || !GGInputRouter.IsDispatchTarget(player, ScpsPanelPackage.Id))
                return;

            bool isOpen = IsOpen(player);
            if (action == GGInputAction.Toggle || isOpen)
                PanelAudioModule.PlaySelection(player);

            if (action == GGInputAction.Toggle)
            {
                if (isOpen) Close(player); else Open(player);
                return;
            }

            if (!isOpen)
                return;
            if (customViewPlayers.Contains(player.UserId))
                HandleCustomInput(player, action);
            else if (locationViewPlayers.Contains(player.UserId))
                HandleLocationInput(player, action);
            else
                HandleMainInput(player, action);
        }

        private void HandleMainInput(Player player, GGInputAction action)
        {
            List<MainEntry> entries = GetMainEntries(player);
            int index = GetMainIndex(player, entries.Count);
            if (action == GGInputAction.Up)
                mainIndexes[player.UserId] = Wrap(index - 1, entries.Count);
            else if (action == GGInputAction.Down)
                mainIndexes[player.UserId] = Wrap(index + 1, entries.Count);
            else if (action == GGInputAction.Confirm || action == GGInputAction.Left)
            {
                MainEntry selected = entries[index];
                if (selected.Kind == MainEntryKind.Switch)
                {
                    Close(player);
                    PanelNavigation.SelectNext(player);
                    return;
                }
                if (selected.Kind == MainEntryKind.Night)
                {
                    ScpsPlayerProgress profile = progress.Get(player);
                    if (!profile.IsNightUnlocked(selected.Night))
                        notices[player.UserId] = L(player, "Clear the previous night first.", "이전 밤을 먼저 클리어해야 합니다.");
                    else if (!plugin.TryStartNight(player, selected.Night, null, out string errorCode))
                        notices[player.UserId] = GetStartError(player, errorCode);
                    else
                    {
                        Close(player);
                        return;
                    }
                }
                else if (selected.Kind == MainEntryKind.Custom)
                {
                    customViewPlayers.Add(player.UserId);
                    locationViewPlayers.Remove(player.UserId);
                    notices.Remove(player.UserId);
                    EnsureCustomLevels(player);
                }
                else if (selected.Kind == MainEntryKind.Locations && player.RemoteAdminAccess)
                {
                    locationViewPlayers.Add(player.UserId);
                    customViewPlayers.Remove(player.UserId);
                    notices.Remove(player.UserId);
                }
            }
            Render(player);
        }

        private void HandleCustomInput(Player player, GGInputAction action)
        {
            if (!progress.Get(player).IsCustomUnlocked)
            {
                customViewPlayers.Remove(player.UserId);
                Render(player);
                return;
            }

            Dictionary<string, int> levels = EnsureCustomLevels(player);
            int index = GetCustomIndex(player);
            if (action == GGInputAction.Up)
                customIndexes[player.UserId] = Wrap(index - 1, CustomBackIndex + 1);
            else if (action == GGInputAction.Down)
                customIndexes[player.UserId] = Wrap(index + 1, CustomBackIndex + 1);
            else if ((action == GGInputAction.Left || action == GGInputAction.Right) && index < ScpNames.Length)
            {
                levels[ScpNames[index]] = WrapLevel(levels[ScpNames[index]] + (action == GGInputAction.Left ? -1 : 1));
                notices.Remove(player.UserId);
            }
            else if (action == GGInputAction.Left && index == CustomBackIndex)
            {
                customViewPlayers.Remove(player.UserId);
                notices.Remove(player.UserId);
            }
            else if (action == GGInputAction.Confirm)
            {
                if (index < ScpNames.Length)
                    levels[ScpNames[index]] = WrapLevel(levels[ScpNames[index]] + 1);
                else if (index == CustomStartIndex)
                {
                    if (!plugin.TryStartNight(player, 0, levels, out string errorCode))
                        notices[player.UserId] = GetStartError(player, errorCode);
                    else
                    {
                        Close(player);
                        return;
                    }
                }
                else
                {
                    customViewPlayers.Remove(player.UserId);
                    notices.Remove(player.UserId);
                }
            }
            Render(player);
        }

        private void HandleLocationInput(Player player, GGInputAction action)
        {
            IReadOnlyList<ScpsLocation> all = locations.All;
            int index = GetLocationIndex(player, all.Count);
            if (!player.RemoteAdminAccess)
            {
                locationViewPlayers.Remove(player.UserId);
                Render(player);
                return;
            }

            if (action == GGInputAction.Up)
                locationIndexes[player.UserId] = Wrap(index - 1, all.Count);
            else if (action == GGInputAction.Down)
                locationIndexes[player.UserId] = Wrap(index + 1, all.Count);
            else if (action == GGInputAction.Right || action == GGInputAction.Left)
            {
                locationViewPlayers.Remove(player.UserId);
                notices.Remove(player.UserId);
            }
            else if (action == GGInputAction.Confirm && all.Count > 0)
            {
                try
                {
                    locations.Capture(all[index].Key, player);
                    notices[player.UserId] = L(player, "Position and direction saved.", "현재 위치와 방향을 저장했습니다.");
                }
                catch (Exception exception)
                {
                    Log.Error($"[SCPS] Location capture failed: {exception}");
                    notices[player.UserId] = L(player, "Save failed. Check the server log.", "저장에 실패했습니다. 서버 로그를 확인하세요.");
                }
            }
            Render(player);
        }

        private void Open(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId))
                return;
            openPlayers.Add(player.UserId);
            customViewPlayers.Remove(player.UserId);
            locationViewPlayers.Remove(player.UserId);
            notices.Remove(player.UserId);
            if (!mainIndexes.ContainsKey(player.UserId)) mainIndexes[player.UserId] = 0;
            EffectDisplayModule.SetPanelOpen(player, true);
            Render(player);
        }

        private void Close(Player player)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId))
                return;
            openPlayers.Remove(player.UserId);
            customViewPlayers.Remove(player.UserId);
            locationViewPlayers.Remove(player.UserId);
            notices.Remove(player.UserId);
            renderer.Clear(player);
            EffectDisplayModule.SetPanelOpen(player, false);
        }

        private void Render(Player player)
        {
            if (!IsOpen(player)) return;
            bool customOpen = customViewPlayers.Contains(player.UserId);
            bool locationsOpen = locationViewPlayers.Contains(player.UserId);
            List<MainEntry> entries = GetMainEntries(player);
            MainEntry mainEntry = entries[GetMainIndex(player, entries.Count)];
            renderer.Render(player, new PanelFrameContent
            {
                Title = IsKorean(player) ? config.PanelTitleKorean : config.PanelTitleEnglish,
                Color = config.PanelColor,
                PageTitle = customOpen ? L(player, "CUSTOM", "커스텀") : locationsOpen ? L(player, "MAP LOCATIONS", "맵 위치 설정") : GetMainPageTitle(player, mainEntry),
                Body = customOpen ? BuildCustomBody(player) : locationsOpen ? BuildLocationBody(player) : BuildMainBody(player, mainEntry),
                Options = customOpen ? BuildCustomOptions(player) : locationsOpen ? BuildLocationOptions(player) : BuildMainOptions(player, entries),
                Controls = customOpen
                    ? L(player, "Up/Down: select   Left/Right: level   Enter: confirm   L: close", "위/아래: 선택   왼쪽/오른쪽: 난이도   Enter: 확인   L: 닫기")
                    : locationsOpen
                        ? L(player, "Up/Down: location   Enter: capture   Left/Right: back   L: close", "위/아래: 위치   Enter: 저장   왼쪽/오른쪽: 뒤로   L: 닫기")
                        : L(player, "Up/Down: option   Enter/Left: select   L: close", "위/아래: 옵션   Enter/왼쪽: 선택   L: 닫기"),
                TopRightSummary = BuildProgressSummary(player),
            });
        }

        private string BuildMainBody(Player player, MainEntry entry)
        {
            if (entry.Kind == MainEntryKind.Switch) return BuildSwitchBody(player);
            if (entry.Kind == MainEntryKind.Night) return BuildNightBody(player, entry.Night);
            if (entry.Kind == MainEntryKind.Custom)
                return $"<size=26><b>{L(player, "CUSTOM", "커스텀")}</b></size>\n\n<size=20>{L(player, "Set every SCP from 1 to 20.", "각 SCP 난이도를 1부터 20까지 설정합니다.")}</size>\n\n<size=17><color=#AAAAAA>{L(player, "Custom runs do not change night progress.", "커스텀 플레이는 밤 진행도를 변경하지 않습니다.")}</color></size>" + BuildNotice(player);
            return BuildMapSummary(player) + BuildNotice(player);
        }

        private string BuildNightBody(Player player, int night)
        {
            ScpsPlayerProgress profile = progress.Get(player);
            ScpsNightPreset preset = ScpsNightPreset.Find(config.NightPresets, night);
            string state = profile.HighestClearedNight >= night ? L(player, "CLEARED", "클리어") : profile.IsNightUnlocked(night) ? L(player, "AVAILABLE", "도전 가능") : L(player, "LOCKED", "잠김");
            string stateColor = profile.HighestClearedNight >= night ? "#80F537" : profile.IsNightUnlocked(night) ? config.PanelColor : "#777777";
            string levels = preset == null ? $"<color=#FF6666>{L(player, "Preset missing", "프리셋 없음")}</color>" : string.Join("\n", ScpNames.Select(name => $"<color=#DD6666>{name}</color>  {preset.GetLevel(name):00}"));
            return $"<size=28><b>{L(player, $"NIGHT {night}", $"{night}일밤")}</b></size>\n<size=18><color={stateColor}><b>{state}</b></color></size>\n\n<size=16>{levels}</size>" + BuildNotice(player);
        }

        private string BuildSwitchBody(Player player)
            => $"<size=28><b>{L(player, "⇄ PANEL SWITCH", "⇄ 패널 전환")}</b></size>\n\n<size=22>{L(player, "Next", "다음")}: <color={config.PanelColor}><b>{Escape(GetNextPanelName(player))}</b></color></size>\n\n<size=17><color=#AAAAAA>{L(player, "Enter or Left Arrow", "Enter 또는 왼쪽 화살표")}</color></size>";

        private string BuildMapSummary(Player player)
            => $"<size=26><b>{L(player, "MAP LOCATIONS", "맵 위치 설정")}</b></size>\n\n<size=19>{L(player, "Save SCPS positions and directions.", "SCPS 위치와 방향을 저장합니다.")}</size>";

        private string BuildMainOptions(Player player, IReadOnlyList<MainEntry> entries)
        {
            int selected = GetMainIndex(player, entries.Count);
            ScpsPlayerProgress profile = progress.Get(player);
            List<string> lines = new List<string>();
            for (int index = 0; index < entries.Count; index++)
            {
                MainEntry entry = entries[index];
                bool current = index == selected;
                string marker = current ? "▶ " : "  ";
                string color = current ? "#80F537" : "#BCBCBC";
                string label;
                if (entry.Kind == MainEntryKind.Switch)
                    label = $"⇄ {L(player, "Switch", "전환")}";
                else if (entry.Kind == MainEntryKind.Night)
                {
                    bool cleared = profile.HighestClearedNight >= entry.Night;
                    bool unlocked = profile.IsNightUnlocked(entry.Night);
                    label = $"{(cleared ? "●" : unlocked ? "○" : "-")} {L(player, $"Night {entry.Night}", $"{entry.Night}일밤")}";
                    if (!current) color = cleared ? "#80F537" : unlocked ? "#DDDDDD" : "#666666";
                }
                else if (entry.Kind == MainEntryKind.Custom)
                {
                    label = $"★ {L(player, "Custom", "커스텀")}";
                    if (!current) color = config.PanelColor;
                }
                else
                {
                    label = $"+ {L(player, "Locations", "위치 설정")}";
                    if (!current) color = "#FF6666";
                }
                lines.Add($"<color={color}>{marker}{label}</color>");
            }
            return "<size=14>" + string.Join("\n", lines) + "</size>";
        }

        private string BuildCustomBody(Player player)
        {
            int index = GetCustomIndex(player);
            Dictionary<string, int> levels = EnsureCustomLevels(player);
            string body;
            if (index < ScpNames.Length)
            {
                string name = ScpNames[index];
                body = $"<size=25><b>{name}</b></size>\n\n<size=38><color={config.PanelColor}><b>{levels[name]:00}</b></color></size>\n\n<size=16><color=#AAAAAA>{L(player, "Range: 1–20", "범위: 1–20")}</color></size>";
            }
            else if (index == CustomStartIndex)
                body = $"<size=27><b>{L(player, "START CUSTOM", "커스텀 시작")}</b></size>\n\n<size=18>{L(player, "Start with these seven values.", "설정한 일곱 난이도로 시작합니다.")}</size>";
            else
                body = $"<size=27><b>{L(player, "BACK", "뒤로")}</b></size>";
            return body + BuildNotice(player);
        }

        private string BuildCustomOptions(Player player)
        {
            int selected = GetCustomIndex(player);
            Dictionary<string, int> levels = EnsureCustomLevels(player);
            List<string> lines = new List<string>();
            for (int index = 0; index < ScpNames.Length; index++)
            {
                string color = index == selected ? "#80F537" : "#BCBCBC";
                lines.Add($"<color={color}>{(index == selected ? "▶ " : "  ")}{ScpNames[index]}  {levels[ScpNames[index]]:00}</color>");
            }
            lines.Add(string.Empty);
            lines.Add($"<color={(selected == CustomStartIndex ? "#80F537" : config.PanelColor)}>{(selected == CustomStartIndex ? "▶ " : "  ")}{L(player, "START", "시작")}</color>");
            lines.Add($"<color={(selected == CustomBackIndex ? "#80F537" : "#AAAAAA")}>{(selected == CustomBackIndex ? "▶ " : "  ")}{L(player, "BACK", "뒤로")}</color>");
            return "<size=13>" + string.Join("\n", lines) + "</size>";
        }

        private string BuildLocationBody(Player player)
        {
            IReadOnlyList<ScpsLocation> all = locations.All;
            if (all.Count == 0) return L(player, "<size=22>No locations registered.</size>", "<size=22>등록된 위치가 없습니다.</size>");
            ScpsLocation selected = all[GetLocationIndex(player, all.Count)];
            string name = IsKorean(player) ? selected.NameKorean : selected.NameEnglish;
            string status = selected.Captured ? L(player, "SAVED", "저장됨") : L(player, "DEFAULT", "기본값");
            return $"<size=22><b>{Escape(name)}</b></size>\n<size=16><color=#AAAAAA>{Escape(selected.Key)}</color></size>\n\n<size=18>{L(player, "Status", "상태")}: <b>{status}</b>\n{L(player, "Position", "위치")}: {Format((UnityEngine.Vector3)selected.Position)}\n{L(player, "Rotation", "회전")}: {Format((UnityEngine.Vector3)selected.Rotation)}</size>" + BuildNotice(player);
        }

        private string BuildLocationOptions(Player player)
        {
            IReadOnlyList<ScpsLocation> all = locations.All;
            if (all.Count == 0) return string.Empty;
            int selected = GetLocationIndex(player, all.Count);
            int start = Math.Max(0, Math.Min(selected - VisibleLocationCount / 2, all.Count - VisibleLocationCount));
            List<string> lines = new List<string>();
            for (int index = start; index < Math.Min(all.Count, start + VisibleLocationCount); index++)
            {
                ScpsLocation location = all[index];
                string mark = location.Captured ? "<color=#80F537>●</color>" : "<color=#888888>○</color>";
                string marker = index == selected ? "<color=#80F537>▶</color> " : "  ";
                lines.Add($"{marker}{mark} {Escape(GetShortLocationName(location, IsKorean(player)))}");
            }
            return "<size=14>" + string.Join("\n", lines) + "</size>";
        }

        private string BuildProgressSummary(Player player)
        {
            ScpsPlayerProgress profile = progress.Get(player);
            string admin = player.RemoteAdminAccess ? $"\n<color=#FF6666><b>{L(player, "ADMIN", "관리자")}</b></color>" : string.Empty;
            return $"<size=16>{L(player, "CLEAR", "클리어")} <color={config.PanelColor}><b>{profile.HighestClearedNight}/6</b></color>{admin}</size>";
        }

        private string BuildNotice(Player player)
            => notices.TryGetValue(player.UserId, out string value) ? $"\n\n<size=16><color=#FFCC55>{Escape(value)}</color></size>" : string.Empty;

        private string GetMainPageTitle(Player player, MainEntry entry)
        {
            if (entry.Kind == MainEntryKind.Switch) return L(player, "⇄ PANEL SWITCH", "⇄ 패널 전환");
            if (entry.Kind == MainEntryKind.Night) return L(player, $"NIGHT {entry.Night}", $"{entry.Night}일밤");
            if (entry.Kind == MainEntryKind.Custom) return L(player, "CUSTOM", "커스텀");
            return L(player, "MAP LOCATIONS", "맵 위치 설정");
        }

        private List<MainEntry> GetMainEntries(Player player)
        {
            ScpsPlayerProgress profile = progress.Get(player);
            List<MainEntry> entries = new List<MainEntry> { new MainEntry(MainEntryKind.Switch) };
            for (int night = 1; night <= 6; night++) entries.Add(new MainEntry(MainEntryKind.Night, night));
            if (profile.IsCustomUnlocked) entries.Add(new MainEntry(MainEntryKind.Custom));
            if (player.RemoteAdminAccess) entries.Add(new MainEntry(MainEntryKind.Locations));
            return entries;
        }

        private Dictionary<string, int> EnsureCustomLevels(Player player)
        {
            if (!customLevels.TryGetValue(player.UserId, out Dictionary<string, int> levels))
            {
                levels = ScpNames.ToDictionary(name => name, _ => 1, StringComparer.OrdinalIgnoreCase);
                customLevels[player.UserId] = levels;
            }
            return levels;
        }

        private string GetStartError(Player player, string errorCode)
        {
            switch (errorCode)
            {
                case "already-starting": return L(player, "A game is already starting.", "이미 게임이 시작 중입니다.");
                case "night-locked": return L(player, "Clear the previous night first.", "이전 밤을 먼저 클리어해야 합니다.");
                case "custom-locked": return L(player, "Clear Night 6 first.", "6일밤을 먼저 클리어해야 합니다.");
                case "preset-missing": return L(player, "This night has no server preset.", "이 밤의 서버 프리셋이 없습니다.");
                default: return L(player, "Could not start the game.", "게임을 시작하지 못했습니다.");
            }
        }

        private void OnLanguageChanged(Player player, string language, bool initial) { if (IsOpen(player)) Render(player); }

        private void OnPanelChanged(Player player, string previous, string current)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.UserId)) return;
            if (string.Equals(current, ScpsPanelPackage.Id, StringComparison.OrdinalIgnoreCase))
            {
                mainIndexes[player.UserId] = 0;
                Open(player);
                return;
            }
            if (string.Equals(previous, ScpsPanelPackage.Id, StringComparison.OrdinalIgnoreCase))
            {
                openPlayers.Remove(player.UserId);
                customViewPlayers.Remove(player.UserId);
                locationViewPlayers.Remove(player.UserId);
                notices.Remove(player.UserId);
                renderer.Clear(player);
            }
        }

        private void OnRoundStarted()
        {
            foreach (Player player in Player.List.Where(player => !player.IsNPC).ToArray()) Close(player);
        }

        private void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            if (ev.Player == null || string.IsNullOrWhiteSpace(ev.Player.UserId)) return;
            Close(ev.Player);
            mainIndexes.Remove(ev.Player.UserId);
            customIndexes.Remove(ev.Player.UserId);
            locationIndexes.Remove(ev.Player.UserId);
            customLevels.Remove(ev.Player.UserId);
        }

        private bool IsOpen(Player player) => player != null && !string.IsNullOrWhiteSpace(player.UserId) && openPlayers.Contains(player.UserId);

        private int GetMainIndex(Player player, int count)
        {
            int index = Wrap(mainIndexes.TryGetValue(player.UserId, out int value) ? value : 0, count);
            mainIndexes[player.UserId] = index;
            return index;
        }

        private int GetCustomIndex(Player player)
        {
            int index = Wrap(customIndexes.TryGetValue(player.UserId, out int value) ? value : 0, CustomBackIndex + 1);
            customIndexes[player.UserId] = index;
            return index;
        }

        private int GetLocationIndex(Player player, int count)
        {
            int index = Wrap(locationIndexes.TryGetValue(player.UserId, out int value) ? value : 0, count);
            locationIndexes[player.UserId] = index;
            return index;
        }

        private string GetNextPanelName(Player player)
        {
            string[] panels = PanelNavigation.GetPanelIds();
            if (panels == null || panels.Length == 0) return "GG";
            int index = Array.FindIndex(panels, id => string.Equals(id, ScpsPanelPackage.Id, StringComparison.OrdinalIgnoreCase));
            return PanelNavigation.GetDisplayName(panels[(Math.Max(0, index) + 1) % panels.Length], IsKorean(player));
        }

        private static string Format(UnityEngine.Vector3 vector) => $"({vector.x:0.###}, {vector.y:0.###}, {vector.z:0.###})";

        private static string GetShortLocationName(ScpsLocation location, bool korean)
        {
            string key = location?.Key ?? string.Empty;
            if (string.Equals(key, "guard.office", StringComparison.OrdinalIgnoreCase)) return korean ? "경비원" : "Guard";
            if (string.Equals(key, "dummy.hidden", StringComparison.OrdinalIgnoreCase)) return korean ? "더미" : "Dummy";
            string[] parts = key.Split('.');
            if (parts.Length >= 2 && parts[0].StartsWith("scp", StringComparison.OrdinalIgnoreCase))
            {
                string scp = FormatScpNumber(parts[0].Substring(3));
                if (string.Equals(parts[1], "spawn", StringComparison.OrdinalIgnoreCase)) return $"{scp} {(korean ? "소환" : "Spawn")}";
                if (string.Equals(parts[1], "dummy", StringComparison.OrdinalIgnoreCase)) return $"{scp} {(korean ? "더미" : "Dummy")}";
                if (parts.Length >= 3 && string.Equals(parts[1], "stage", StringComparison.OrdinalIgnoreCase)) return $"{scp} · {parts[2]}";
                if (parts.Length >= 3 && string.Equals(parts[1], "run", StringComparison.OrdinalIgnoreCase)) return $"{scp} {(korean ? "돌진" : "Run")} {parts[2]}";
            }
            string fallback = korean ? location?.NameKorean : location?.NameEnglish;
            fallback ??= key;
            return fallback.Length <= 14 ? fallback : fallback.Substring(0, 13) + "…";
        }

        private static string FormatScpNumber(string number) => string.Equals(number, "0492", StringComparison.OrdinalIgnoreCase) ? "049-2" : number;

        private static int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            int result = value % count;
            return result < 0 ? result + count : result;
        }

        private static int WrapLevel(int value) => value < 1 ? 20 : value > 20 ? 1 : value;
        private static bool IsKorean(Player player) => CommonSettingsModule.IsKorean(player);
        private static string L(Player player, string english, string korean) => IsKorean(player) ? korean : english;
        private static string Escape(string value) => (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private void ClearSessions()
        {
            openPlayers.Clear();
            customViewPlayers.Clear();
            locationViewPlayers.Clear();
            mainIndexes.Clear();
            customIndexes.Clear();
            locationIndexes.Clear();
            customLevels.Clear();
            notices.Clear();
        }

        private enum MainEntryKind { Switch, Night, Custom, Locations }

        private sealed class MainEntry
        {
            public MainEntry(MainEntryKind kind, int night = 0) { Kind = kind; Night = night; }
            public MainEntryKind Kind { get; }
            public int Night { get; }
        }
    }
}
