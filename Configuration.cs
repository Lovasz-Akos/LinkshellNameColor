using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;
using LinkshellNameColor.Models;

namespace LinkshellNameColor
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool PluginEnabled { get; set; } = true;
        public bool ColorizeName { get; set; } = true;
        public bool OverruleFriendlistColor { get; set; } = true;
        public bool ColorizeTitle { get; set; } = false;
        public bool ColorizeFreeCompany { get; set; } = false;
        public bool AppendLinkshellTag { get; set; } = false;
        public bool TagIsPrefix { get; set; } = false;
        public bool PrioritizeStandardOverCrossWorld { get; set; } = true;
        public bool MatchWorldName { get; set; } = false;
        public bool VerboseLogging { get; set; } = false;

        public List<LinkshellChannelConfig> LinkshellChannels { get; set; } = new();

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            EnsureDefaultChannels();
        }

        public void EnsureDefaultChannels()
        {
            if (LinkshellChannels.Count == 0)
            {
                LinkshellChannels.Add(new LinkshellChannelConfig("LS1", "Linkshell 1", LinkshellType.StandardLinkshell, 1, new Vector4(0.00f, 0.90f, 1.00f, 1.00f), 571));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS2", "Linkshell 2", LinkshellType.StandardLinkshell, 2, new Vector4(1.00f, 0.85f, 0.20f, 1.00f), 572));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS3", "Linkshell 3", LinkshellType.StandardLinkshell, 3, new Vector4(1.00f, 0.45f, 0.45f, 1.00f), 573));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS4", "Linkshell 4", LinkshellType.StandardLinkshell, 4, new Vector4(0.20f, 0.90f, 0.45f, 1.00f), 574));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS5", "Linkshell 5", LinkshellType.StandardLinkshell, 5, new Vector4(0.75f, 0.45f, 1.00f, 1.00f), 575));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS6", "Linkshell 6", LinkshellType.StandardLinkshell, 6, new Vector4(1.00f, 0.60f, 0.15f, 1.00f), 576));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS7", "Linkshell 7", LinkshellType.StandardLinkshell, 7, new Vector4(1.00f, 0.35f, 0.75f, 1.00f), 577));
                LinkshellChannels.Add(new LinkshellChannelConfig("LS8", "Linkshell 8", LinkshellType.StandardLinkshell, 8, new Vector4(0.20f, 0.70f, 1.00f, 1.00f), 579));

                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS1", "CW Linkshell 1", LinkshellType.CrossWorldLinkshell, 1, new Vector4(0.00f, 0.90f, 1.00f, 1.00f), 571));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS2", "CW Linkshell 2", LinkshellType.CrossWorldLinkshell, 2, new Vector4(0.20f, 0.70f, 1.00f, 1.00f), 579));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS3", "CW Linkshell 3", LinkshellType.CrossWorldLinkshell, 3, new Vector4(1.00f, 0.35f, 0.75f, 1.00f), 577));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS4", "CW Linkshell 4", LinkshellType.CrossWorldLinkshell, 4, new Vector4(1.00f, 0.85f, 0.20f, 1.00f), 572));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS5", "CW Linkshell 5", LinkshellType.CrossWorldLinkshell, 5, new Vector4(0.20f, 0.90f, 0.45f, 1.00f), 574));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS6", "CW Linkshell 6", LinkshellType.CrossWorldLinkshell, 6, new Vector4(0.75f, 0.45f, 1.00f, 1.00f), 575));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS7", "CW Linkshell 7", LinkshellType.CrossWorldLinkshell, 7, new Vector4(1.00f, 0.35f, 0.75f, 1.00f), 577));
                LinkshellChannels.Add(new LinkshellChannelConfig("CWLS8", "CW Linkshell 8", LinkshellType.CrossWorldLinkshell, 8, new Vector4(0.20f, 0.70f, 1.00f, 1.00f), 579));

                Save();
            }
        }

        public void Save()
        {
            pluginInterface?.SavePluginConfig(this);
        }
    }
}
