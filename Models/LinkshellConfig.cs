using System;
using System.Collections.Generic;
using System.Numerics;

namespace LinkshellNameColor.Models
{
    public enum LinkshellType
    {
        StandardLinkshell,
        CrossWorldLinkshell
    }

    public class LinkshellChannelConfig
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public LinkshellType Type { get; set; } = LinkshellType.StandardLinkshell;
        public int ChannelNumber { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public Vector4 Color { get; set; } = new Vector4(1f, 1f, 1f, 1f);
        public ushort UIColorId { get; set; } = 571;
        public HashSet<string> Members { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public LinkshellChannelConfig() { }

        public LinkshellChannelConfig(string key, string label, LinkshellType type, int number, Vector4 color, ushort uiColorId)
        {
            Key = key;
            Label = label;
            Type = type;
            ChannelNumber = number;
            Color = color;
            UIColorId = uiColorId;
        }
    }

    public class PlayerLinkshellMatch
    {
        public string PlayerName { get; set; } = string.Empty;
        public string WorldName { get; set; } = string.Empty;
        public LinkshellChannelConfig ChannelConfig { get; set; } = null!;
        public string BadgeTag { get; set; } = string.Empty;
    }
}
