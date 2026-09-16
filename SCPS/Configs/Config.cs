using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Interfaces;

namespace SCPS
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [Description("1일밤부터 6일밤까지 적용할 SCP별 난이도 프리셋입니다. 난이도는 0~20이며 0은 해당 SCP를 비활성화합니다.")]
        public List<ScpsNightPreset> NightPresets { get; set; } = ScpsNightPreset.CreateDefaults();

        [Description("GG SCPS panel title shown to English users.")]
        public string PanelTitleEnglish { get; set; } = "SCPS";

        [Description("GG SCPS 패널에서 한국어 사용자에게 표시할 제목입니다.")]
        public string PanelTitleKorean { get; set; } = "SCP와 재단";

        [Description("GG SCPS panel accent color.")]
        public string PanelColor { get; set; } = "#B586FD";

        [Description("GG panel navigation order.")]
        public int PanelSortOrder { get; set; } = 30;
    }
}
