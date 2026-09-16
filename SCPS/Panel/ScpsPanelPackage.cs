using GG.Core.Modules;
using GG.Core.Panel;
using SCPS.Locations;
using SCPS.Progress;

namespace SCPS.Panel
{
    public sealed class ScpsPanelPackage
    {
        public const string Id = "SCPS";

        private readonly Config config;
        private readonly ScpsLocationService locations;
        private readonly ScpsProgressStore progress;
        private readonly SCPS plugin;

        public ScpsPanelPackage(Config config, ScpsLocationService locations, ScpsProgressStore progress, SCPS plugin)
        {
            this.config = config;
            this.locations = locations;
            this.progress = progress;
            this.plugin = plugin;
        }

        public PanelPackageRuntime Create()
            => new PanelPackageRuntime(
                Id,
                "SCPS",
                locations.RootDirectory,
                locations,
                new IServerModule[] { new ScpsPanelModule(config, locations, progress, plugin) },
                config.PanelTitleEnglish,
                config.PanelTitleKorean,
                config.PanelColor,
                config.PanelSortOrder);
    }
}
