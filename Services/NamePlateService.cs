using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Lumina.Text.ReadOnly;

namespace LinkshellNameColor.Services
{
    public class NamePlateService : IDisposable
    {
        private readonly INamePlateGui namePlateGui;
        private readonly Configuration configuration;
        private readonly LinkshellManager linkshellManager;
        private readonly IPluginLog log;
        private readonly IChatGui chatGui;

        private int debugDumpCountdown = 0;

        public NamePlateService(INamePlateGui namePlateGui, Configuration configuration, LinkshellManager linkshellManager, IPluginLog log, IChatGui chatGui)
        {
            this.namePlateGui = namePlateGui;
            this.configuration = configuration;
            this.linkshellManager = linkshellManager;
            this.log = log;
            this.chatGui = chatGui;

            this.namePlateGui.OnNamePlateUpdate += OnNamePlateUpdate;
        }

        public void TriggerDebugDump()
        {
            debugDumpCountdown = 15;
            log.Info("NamePlateService: Live debug dump triggered.");
            chatGui.Print("[LinkshellNameColor] Capturing live nameplate debug data... Check Dalamud log (/xilog).");
            RequestRedraw();
        }

        public void RequestRedraw()
        {
            try
            {
                namePlateGui.RequestRedraw();
            }
            catch (Exception ex)
            {
                log.Debug(ex, "NamePlateService: Failed to request nameplate redraw.");
            }
        }

        private void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
        {
            if (!configuration.PluginEnabled && debugDumpCountdown <= 0) return;

            try
            {
                foreach (var handler in handlers)
                {
                    // Only process Player Characters
                    if (handler.NamePlateKind != NamePlateKind.PlayerCharacter) continue;

                    var playerChar = handler.PlayerCharacter;
                    if (playerChar == null) continue;

                    string playerName = playerChar.Name.TextValue;
                    if (string.IsNullOrWhiteSpace(playerName)) continue;

                    string? worldName = playerChar.HomeWorld.ValueNullable?.Name.ExtractText();
                    var match = linkshellManager.GetMatch(playerName, worldName);

                    // Debug Logger
                    if (debugDumpCountdown > 0)
                    {
                        debugDumpCountdown--;
                        log.Info($"[Debug] Player: {playerName}, Match: {match?.ChannelConfig.Key ?? "None"}, TextColor: 0x{handler.TextColor:X8}, EdgeColor: 0x{handler.EdgeColor:X8}");
                    }

                    if (!configuration.PluginEnabled || match == null || !match.ChannelConfig.Enabled) continue;

                    // Convert ImGui Vector4 (RGBA) to FFXIV Native ABGR 32-bit uint color
                    uint abgrColor = ConvertVector4ToAbgr(match.ChannelConfig.Color);
                    uint edgeAbgrColor = CalculateEdgeColor(abgrColor);

                    // 1. Set Native ABGR 32-bit integer Text Color and Edge Color
                    handler.TextColor = abgrColor;
                    handler.EdgeColor = edgeAbgrColor;

                    // 2. Append Linkshell Tag to Name if configured (Without adding color payloads that corrupt TextColor)
                    if (configuration.AppendLinkshellTag)
                    {
                        string badge = $"[{match.BadgeTag}]";
                        var newName = new SeString();
                        if (configuration.TagIsPrefix)
                        {
                            newName.Payloads.Add(new TextPayload($"{badge} "));
                            newName.Payloads.AddRange(handler.Name.Payloads);
                        }
                        else
                        {
                            newName.Payloads.AddRange(handler.Name.Payloads);
                            newName.Payloads.Add(new TextPayload($" [{badge}]"));
                        }
                        handler.Name = newName;
                    }
                }
            }
            catch (Exception ex)
            {
                if (configuration.VerboseLogging)
                {
                    log.Error(ex, "NamePlateService: Error during NamePlateUpdate event.");
                }
            }
        }

        public static uint ConvertVector4ToAbgr(Vector4 color)
        {
            byte r = (byte)Math.Clamp((int)(color.X * 255f), 0, 255);
            byte g = (byte)Math.Clamp((int)(color.Y * 255f), 0, 255);
            byte b = (byte)Math.Clamp((int)(color.Z * 255f), 0, 255);
            byte a = (byte)Math.Clamp((int)(color.W * 255f), 0, 255);
            if (a == 0) a = 255;

            // ABGR uint bit packing: 0xAABBGGRR
            return (uint)((a << 24) | (b << 16) | (g << 8) | r);
        }

        public static uint CalculateEdgeColor(uint abgrColor)
        {
            byte r = (byte)(abgrColor & 0xFF);
            byte g = (byte)((abgrColor >> 8) & 0xFF);
            byte b = (byte)((abgrColor >> 16) & 0xFF);
            byte a = (byte)((abgrColor >> 24) & 0xFF);

            // Calculate dark complementary outline (35% brightness)
            byte edgeR = (byte)(r * 0.35f);
            byte edgeG = (byte)(g * 0.35f);
            byte edgeB = (byte)(b * 0.35f);

            return (uint)((a << 24) | (edgeB << 16) | (edgeG << 8) | edgeR);
        }

        public void Dispose()
        {
            this.namePlateGui.OnNamePlateUpdate -= OnNamePlateUpdate;
        }
    }
}
