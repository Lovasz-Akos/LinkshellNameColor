using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using LinkshellNameColor.Models;
using LinkshellNameColor.Services;

namespace LinkshellNameColor.UI
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;
        private readonly LinkshellManager linkshellManager;
        private readonly NamePlateService namePlateService;

        private string newMemberInput = string.Empty;
        private string memberSearchFilter = string.Empty;
        private string batchImportText = string.Empty;
        private int selectedChannelIndex = 0;
        private bool showBatchImportModal = false;

        public ConfigWindow(Configuration configuration, LinkshellManager linkshellManager, NamePlateService namePlateService) 
            : base("LinkshellNameColor Configuration###LinkshellNameColor_Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.configuration = configuration;
            this.linkshellManager = linkshellManager;
            this.namePlateService = namePlateService;

            Size = new Vector2(680, 560);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.4f, 1f), $"Status: {(configuration.PluginEnabled ? "ACTIVE" : "DISABLED")}");
            ImGui.SameLine();
            ImGui.TextDisabled($"|  Tracked Members: {linkshellManager.TotalTrackedMembers}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Force Redraw Nameplates"))
            {
                namePlateService.RequestRedraw();
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Dump Live Debug Data"))
            {
                namePlateService.TriggerDebugDump();
            }

            ImGui.Spacing();

            if (ImGui.BeginTabBar("LinkshellNameColor_TabBar"))
            {
                if (ImGui.BeginTabItem("Linkshell Colors"))
                {
                    DrawColorsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Member Rosters"))
                {
                    DrawRostersTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Display Settings"))
                {
                    DrawSettingsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Presets & Onboarding"))
                {
                    DrawPresetsTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawColorsTab()
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Assign custom colors for each Linkshell (LS1–8) and Cross-World Linkshell (CWLS1–8):");
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginChild("ColorsChildFrame", new Vector2(0, -35), true))
            {
                if (ImGui.CollapsingHeader("Standard Linkshells (LS1 – LS8)", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var stdChannels = configuration.LinkshellChannels.Where(c => c.Type == LinkshellType.StandardLinkshell).ToList();
                    DrawChannelColorTable(stdChannels);
                }

                ImGui.Spacing();

                if (ImGui.CollapsingHeader("Cross-World Linkshells (CWLS1 – CWLS8)", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var cwChannels = configuration.LinkshellChannels.Where(c => c.Type == LinkshellType.CrossWorldLinkshell).ToList();
                    DrawChannelColorTable(cwChannels);
                }

                ImGui.EndChild();
            }

            if (ImGui.Button("Save Configuration & Redraw"))
            {
                configuration.Save();
                linkshellManager.RebuildLookupCache();
                namePlateService.RequestRedraw();
            }
        }

        private void DrawChannelColorTable(System.Collections.Generic.List<LinkshellChannelConfig> channels)
        {
            if (ImGui.BeginTable("ColorsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn("Color Picker", ImGuiTableColumnFlags.WidthFixed, 220f);
                ImGui.TableSetupColumn("Nameplate Preview", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var channel in channels)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    bool enabled = channel.Enabled;
                    if (ImGui.Checkbox($"##Enabled_{channel.Key}", ref enabled))
                    {
                        channel.Enabled = enabled;
                        configuration.Save();
                        linkshellManager.RebuildLookupCache();
                        namePlateService.RequestRedraw();
                    }

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{channel.Key} ({channel.Label})");

                    ImGui.TableNextColumn();
                    Vector4 color = channel.Color;
                    if (ImGui.ColorEdit4($"##Color_{channel.Key}", ref color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview))
                    {
                        channel.Color = color;
                        configuration.Save();
                        namePlateService.RequestRedraw();
                    }

                    ImGui.TableNextColumn();
                    string badge = $"[{channel.Key}]";
                    string previewText = configuration.TagIsPrefix ? $"{badge} Sample Character" : $"Sample Character {badge}";
                    ImGui.TextColored(channel.Color, previewText);
                }

                ImGui.EndTable();
            }
        }

        private void DrawRostersTab()
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Manage Linkshell member lists for automatic nameplate recoloring:");
            ImGui.Spacing();

            var channelKeys = configuration.LinkshellChannels.Select(c => $"{c.Key} ({c.Label}) - {c.Members.Count} members").ToArray();
            if (selectedChannelIndex >= channelKeys.Length) selectedChannelIndex = 0;

            ImGui.SetNextItemWidth(300);
            if (ImGui.Combo("Target Channel", ref selectedChannelIndex, channelKeys, channelKeys.Length))
            {
                newMemberInput = string.Empty;
            }

            var currentChannel = configuration.LinkshellChannels[selectedChannelIndex];

            ImGui.SameLine();
            if (ImGui.Button("Scan In-Game Memory"))
            {
                linkshellManager.ScanInGameLinkshells(currentChannel.Key);
                namePlateService.RequestRedraw();
            }

            ImGui.SameLine();
            if (ImGui.Button("Batch Paste Names"))
            {
                showBatchImportModal = !showBatchImportModal;
            }

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextUnformatted("Add Single Member:");
            ImGui.SetNextItemWidth(220);
            ImGui.InputText($"##AddMemberInput_{currentChannel.Key}", ref newMemberInput, 64);
            ImGui.SameLine();
            if (ImGui.Button("Add Member") && !string.IsNullOrWhiteSpace(newMemberInput))
            {
                if (linkshellManager.AddMember(currentChannel.Key, newMemberInput))
                {
                    newMemberInput = string.Empty;
                    namePlateService.RequestRedraw();
                }
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(160);
            ImGui.InputText("Filter##SearchFilter", ref memberSearchFilter, 32);

            ImGui.Spacing();

            if (showBatchImportModal)
            {
                if (ImGui.CollapsingHeader($"Batch Paste Names into {currentChannel.Key}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.TextDisabled("Paste character names below (one per line, e.g. 'First Last'):");
                    ImGui.InputTextMultiline("##BatchImportText", ref batchImportText, 4096, new Vector2(-1, 80));
                    if (ImGui.Button("Add All Pasted Names to Channel"))
                    {
                        var lines = batchImportText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        int added = 0;
                        foreach (var line in lines)
                        {
                            if (linkshellManager.AddMember(currentChannel.Key, line)) added++;
                        }
                        batchImportText = string.Empty;
                        showBatchImportModal = false;
                        namePlateService.RequestRedraw();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel")) showBatchImportModal = false;
                    ImGui.Separator();
                }
            }

            if (ImGui.CollapsingHeader("Quick-Add Nearby Players in Zone", ImGuiTreeNodeFlags.None))
            {
                var nearby = linkshellManager.GetNearbyPlayerNames();
                if (nearby.Count == 0)
                {
                    ImGui.TextDisabled("No other player characters visible nearby in current zone.");
                }
                else
                {
                    ImGui.TextDisabled("Click '+ Add' next to any nearby player to assign them to the current channel:");
                    if (ImGui.BeginChild("NearbyChild", new Vector2(0, 100), true))
                    {
                        foreach (var pName in nearby)
                        {
                            bool alreadyInChannel = currentChannel.Members.Contains(pName);
                            if (alreadyInChannel)
                            {
                                ImGui.TextDisabled($"✓ {pName} (In {currentChannel.Key})");
                            }
                            else
                            {
                                if (ImGui.Button($"+ Add to {currentChannel.Key}##{pName}"))
                                {
                                    linkshellManager.AddMember(currentChannel.Key, pName);
                                    namePlateService.RequestRedraw();
                                }
                                ImGui.SameLine();
                                ImGui.TextUnformatted(pName);
                            }
                        }
                        ImGui.EndChild();
                    }
                }
            }

            ImGui.Spacing();

            if (ImGui.BeginChild("RosterListChild", new Vector2(0, -35), true))
            {
                var membersList = currentChannel.Members
                    .Where(m => string.IsNullOrWhiteSpace(memberSearchFilter) || m.Contains(memberSearchFilter, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m)
                    .ToList();

                if (membersList.Count == 0)
                {
                    ImGui.TextDisabled("No members registered in this channel.");
                    ImGui.BulletText("Option 1: Add names using 'Add Single Member' or 'Batch Paste Names' above.");
                    ImGui.BulletText("Option 2: Open your Social -> Linkshell window in-game, then click 'Scan In-Game Memory'.");
                    ImGui.BulletText("Option 3: Use 'Quick-Add Nearby Players' to add friends standing near you with 1 click.");
                }
                else
                {
                    if (ImGui.BeginTable("RosterTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                    {
                        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 40f);
                        ImGui.TableSetupColumn("Character Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 90f);
                        ImGui.TableHeadersRow();

                        int index = 1;
                        foreach (var memberName in membersList)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.TextDisabled(index++.ToString());

                            ImGui.TableNextColumn();
                            ImGui.TextColored(currentChannel.Color, memberName);

                            ImGui.TableNextColumn();
                            if (ImGui.Button($"Remove##{memberName}"))
                            {
                                linkshellManager.RemoveMember(currentChannel.Key, memberName);
                                namePlateService.RequestRedraw();
                                break;
                            }
                        }

                        ImGui.EndTable();
                    }
                }

                ImGui.EndChild();
            }

            if (ImGui.Button("Clear All Members in Channel"))
            {
                linkshellManager.ClearChannelMembers(currentChannel.Key);
                namePlateService.RequestRedraw();
            }
        }

        private void DrawSettingsTab()
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("General Display & Recolor Rules:");
            ImGui.Separator();
            ImGui.Spacing();

            bool enabled = configuration.PluginEnabled;
            if (ImGui.Checkbox("Enable LinkshellNameColor Plugin", ref enabled))
            {
                configuration.PluginEnabled = enabled;
                configuration.Save();
                namePlateService.RequestRedraw();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool colorName = configuration.ColorizeName;
            if (ImGui.Checkbox("Recolor Player Character Name", ref colorName))
            {
                configuration.ColorizeName = colorName;
                configuration.Save();
                namePlateService.RequestRedraw();
            }

            bool overruleFriend = configuration.OverruleFriendlistColor;
            if (ImGui.Checkbox("Overrule Friendlist Orange / Default Nameplate Colors (Recommended)", ref overruleFriend))
            {
                configuration.OverruleFriendlistColor = overruleFriend;
                configuration.Save();
                namePlateService.RequestRedraw();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("When enabled, Linkshell colors take strict precedence over standard Friendlist Orange, Party Blue, and FC Green nameplate colors.");
            }

            bool colorTitle = configuration.ColorizeTitle;
            if (ImGui.Checkbox("Recolor Player Title Text", ref colorTitle))
            {
                configuration.ColorizeTitle = colorTitle;
                configuration.Save();
                namePlateService.RequestRedraw();
            }

            bool colorFC = configuration.ColorizeFreeCompany;
            if (ImGui.Checkbox("Recolor Free Company Tag Text", ref colorFC))
            {
                configuration.ColorizeFreeCompany = colorFC;
                configuration.Save();
                namePlateService.RequestRedraw();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool appendTag = configuration.AppendLinkshellTag;
            if (ImGui.Checkbox("Append Linkshell Badge Tag (e.g. [LS1] / [CWLS3])", ref appendTag))
            {
                configuration.AppendLinkshellTag = appendTag;
                configuration.Save();
                namePlateService.RequestRedraw();
            }

            if (configuration.AppendLinkshellTag)
            {
                ImGui.Indent();
                bool isPrefix = configuration.TagIsPrefix;
                if (ImGui.RadioButton("Prefix (e.g. [LS1] First Last)", isPrefix))
                {
                    configuration.TagIsPrefix = true;
                    configuration.Save();
                    namePlateService.RequestRedraw();
                }
                ImGui.SameLine();
                if (ImGui.RadioButton("Suffix (e.g. First Last [LS1])", !isPrefix))
                {
                    configuration.TagIsPrefix = false;
                    configuration.Save();
                    namePlateService.RequestRedraw();
                }
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool prioStd = configuration.PrioritizeStandardOverCrossWorld;
            if (ImGui.Checkbox("Prioritize Standard Linkshells over Cross-World Linkshells when matching", ref prioStd))
            {
                configuration.PrioritizeStandardOverCrossWorld = prioStd;
                configuration.Save();
                linkshellManager.RebuildLookupCache();
                namePlateService.RequestRedraw();
            }

            bool matchWorld = configuration.MatchWorldName;
            if (ImGui.Checkbox("Require World Name Match (e.g. First Last@Server)", ref matchWorld))
            {
                configuration.MatchWorldName = matchWorld;
                configuration.Save();
                linkshellManager.RebuildLookupCache();
                namePlateService.RequestRedraw();
            }

            bool verbose = configuration.VerboseLogging;
            if (ImGui.Checkbox("Verbose Debug Logging", ref verbose))
            {
                configuration.VerboseLogging = verbose;
                configuration.Save();
            }
        }

        private void DrawPresetsTab()
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Color Presets & Quick Setup:");
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextWrapped("Choose a pre-designed palette to quickly recolor all Linkshell channels with harmonious, high-visibility tones:");
            ImGui.Spacing();

            if (ImGui.Button("Apply Vibrant Neon Palette", new Vector2(220, 32)))
            {
                ApplyPalettePreset(1);
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Bright, high-contrast colors perfect for crowded hunt trains and cities.");

            ImGui.Spacing();

            if (ImGui.Button("Apply Soft Pastel Palette", new Vector2(220, 32)))
            {
                ApplyPalettePreset(2);
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Gentle pastel shades that blend softly with the game UI.");

            ImGui.Spacing();

            if (ImGui.Button("Apply Classic FC Colors", new Vector2(220, 32)))
            {
                ApplyPalettePreset(3);
            }
            ImGui.SameLine();
            ImGui.TextDisabled("Classic gold, emerald, and azure colors inspired by standard FC & Party tags.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Reset All Settings & Colors to Defaults", new Vector2(260, 32)))
            {
                configuration.LinkshellChannels.Clear();
                configuration.EnsureDefaultChannels();
                linkshellManager.RebuildLookupCache();
                namePlateService.RequestRedraw();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), "Onboarding Quick Start Guide:");
            ImGui.BulletText("Type /lsnc or /linkshellnamecolor in chat to toggle this configuration window.");
            ImGui.BulletText("Open Social -> Linkshell in-game to populate member lists, or click 'Quick-Add Nearby Players' / 'Batch Paste Names'.");
            ImGui.BulletText("Assign unique colors to each Linkshell channel to instantly spot friends in crowds.");
        }

        private void ApplyPalettePreset(int presetId)
        {
            if (presetId == 1)
            {
                for (int i = 0; i < configuration.LinkshellChannels.Count; i++)
                {
                    var ch = configuration.LinkshellChannels[i];
                    ch.Color = (i % 8) switch
                    {
                        0 => new Vector4(0.00f, 1.00f, 1.00f, 1.00f),
                        1 => new Vector4(1.00f, 0.90f, 0.00f, 1.00f),
                        2 => new Vector4(1.00f, 0.20f, 0.50f, 1.00f),
                        3 => new Vector4(0.00f, 1.00f, 0.40f, 1.00f),
                        4 => new Vector4(0.80f, 0.20f, 1.00f, 1.00f),
                        5 => new Vector4(1.00f, 0.50f, 0.00f, 1.00f),
                        6 => new Vector4(0.20f, 0.60f, 1.00f, 1.00f),
                        _ => new Vector4(0.60f, 1.00f, 0.00f, 1.00f),
                    };
                }
            }
            else if (presetId == 2)
            {
                for (int i = 0; i < configuration.LinkshellChannels.Count; i++)
                {
                    var ch = configuration.LinkshellChannels[i];
                    ch.Color = (i % 8) switch
                    {
                        0 => new Vector4(0.60f, 0.90f, 0.95f, 1.00f),
                        1 => new Vector4(0.95f, 0.90f, 0.65f, 1.00f),
                        2 => new Vector4(0.95f, 0.70f, 0.75f, 1.00f),
                        3 => new Vector4(0.65f, 0.90f, 0.75f, 1.00f),
                        4 => new Vector4(0.80f, 0.75f, 0.95f, 1.00f),
                        5 => new Vector4(0.95f, 0.80f, 0.65f, 1.00f),
                        6 => new Vector4(0.70f, 0.80f, 0.95f, 1.00f),
                        _ => new Vector4(0.85f, 0.95f, 0.65f, 1.00f),
                    };
                }
            }
            else if (presetId == 3)
            {
                for (int i = 0; i < configuration.LinkshellChannels.Count; i++)
                {
                    var ch = configuration.LinkshellChannels[i];
                    ch.Color = (i % 8) switch
                    {
                        0 => new Vector4(1.00f, 0.85f, 0.30f, 1.00f),
                        1 => new Vector4(0.30f, 0.85f, 0.60f, 1.00f),
                        2 => new Vector4(0.40f, 0.70f, 1.00f, 1.00f),
                        3 => new Vector4(0.90f, 0.40f, 0.40f, 1.00f),
                        4 => new Vector4(0.75f, 0.50f, 0.90f, 1.00f),
                        5 => new Vector4(0.95f, 0.60f, 0.25f, 1.00f),
                        6 => new Vector4(0.20f, 0.85f, 0.85f, 1.00f),
                        _ => new Vector4(0.80f, 0.80f, 0.80f, 1.00f),
                    };
                }
            }

            configuration.Save();
            linkshellManager.RebuildLookupCache();
            namePlateService.RequestRedraw();
        }

        public void Dispose()
        {
        }
    }
}
