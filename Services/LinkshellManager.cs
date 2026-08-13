using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using LinkshellNameColor.Models;

namespace LinkshellNameColor.Services
{
    public class LinkshellManager
    {
        private readonly Configuration configuration;
        private readonly IPluginLog log;
        private readonly IClientState clientState;
        private readonly IObjectTable objectTable;

        private readonly Dictionary<string, PlayerLinkshellMatch> memberLookup = new(StringComparer.OrdinalIgnoreCase);

        public int TotalTrackedMembers { get; private set; } = 0;
        public DateTime LastScanTime { get; private set; } = DateTime.MinValue;

        public LinkshellManager(Configuration configuration, IPluginLog log, IClientState clientState, IObjectTable objectTable)
        {
            this.configuration = configuration;
            this.log = log;
            this.clientState = clientState;
            this.objectTable = objectTable;

            RebuildLookupCache();
        }

        public void RebuildLookupCache()
        {
            memberLookup.Clear();
            int count = 0;

            var channels = configuration.LinkshellChannels
                .Where(c => c.Enabled)
                .OrderBy(c => configuration.PrioritizeStandardOverCrossWorld 
                    ? (c.Type == LinkshellType.StandardLinkshell ? 0 : 1) 
                    : (c.Type == LinkshellType.CrossWorldLinkshell ? 0 : 1))
                .ThenBy(c => c.ChannelNumber);

            foreach (var channel in channels)
            {
                string tag = channel.Type == LinkshellType.StandardLinkshell ? $"LS{channel.ChannelNumber}" : $"CWLS{channel.ChannelNumber}";
                
                foreach (var rawName in channel.Members)
                {
                    if (string.IsNullOrWhiteSpace(rawName)) continue;

                    string cleanName = rawName.Trim();
                    string lookupKey = cleanName;

                    if (!memberLookup.ContainsKey(lookupKey))
                    {
                        var match = new PlayerLinkshellMatch
                        {
                            PlayerName = cleanName,
                            ChannelConfig = channel,
                            BadgeTag = tag
                        };
                        memberLookup[lookupKey] = match;
                        count++;
                    }
                }
            }

            TotalTrackedMembers = count;
            if (configuration.VerboseLogging)
            {
                log.Debug($"LinkshellManager: Rebuilt lookup cache with {count} unique entries.");
            }
        }

        public PlayerLinkshellMatch? GetMatch(string playerName, string? worldName = null)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return null;

            string searchKey = playerName.Trim();
            
            if (configuration.MatchWorldName && !string.IsNullOrWhiteSpace(worldName))
            {
                string fullKey = $"{searchKey}@{worldName.Trim()}";
                if (memberLookup.TryGetValue(fullKey, out var fullMatch))
                {
                    return fullMatch;
                }
            }

            if (memberLookup.TryGetValue(searchKey, out var match))
            {
                return match;
            }

            return null;
        }

        public List<string> GetNearbyPlayerNames()
        {
            var names = new List<string>();
            try
            {
                foreach (var obj in objectTable)
                {
                    if (obj is IPlayerCharacter playerChar)
                    {
                        string pName = playerChar.Name.TextValue;
                        if (!string.IsNullOrWhiteSpace(pName) && !names.Contains(pName))
                        {
                            names.Add(pName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex, "LinkshellManager: Error scanning objectTable.");
            }
            return names.OrderBy(n => n).ToList();
        }

        public unsafe bool ScanInGameLinkshells(string? targetChannelKey = null)
        {
            if (!clientState.IsLoggedIn) return false;

            try
            {
                int importedCount = 0;

                var infoModule = InfoModule.Instance();
                var agentModule = AgentModule.Instance();

                string lsTargetKey = "LS1";
                if (!string.IsNullOrWhiteSpace(targetChannelKey) && targetChannelKey.StartsWith("LS", StringComparison.OrdinalIgnoreCase))
                {
                    lsTargetKey = targetChannelKey.ToUpperInvariant();
                }
                else if (agentModule != null)
                {
                    var lsAgent = agentModule->GetAgentLinkshell();
                    if (lsAgent != null)
                    {
                        int lsNum = lsAgent->SelectedLSIndex + 1;
                        if (lsNum >= 1 && lsNum <= 8) lsTargetKey = $"LS{lsNum}";
                    }
                }

                string cwlsTargetKey = "CWLS1";
                if (!string.IsNullOrWhiteSpace(targetChannelKey) && targetChannelKey.StartsWith("CWLS", StringComparison.OrdinalIgnoreCase))
                {
                    cwlsTargetKey = targetChannelKey.ToUpperInvariant();
                }
                else if (agentModule != null)
                {
                    var cwlsAgent = agentModule->GetAgentCrossWorldLinkshell();
                    if (cwlsAgent != null)
                    {
                        int cwNum = cwlsAgent->SelectedCWLSIndex + 1;
                        if (cwNum >= 1 && cwNum <= 8) cwlsTargetKey = $"CWLS{cwNum}";
                    }
                }

                if (infoModule != null)
                {
                    var lsMemberProxy = (InfoProxyCommonList*)infoModule->GetInfoProxyById(InfoProxyId.LinkshellMember);
                    if (lsMemberProxy != null && lsMemberProxy->EntryCount > 0)
                    {
                        var entries = lsMemberProxy->CharDataSpan;
                        foreach (ref readonly var charData in entries)
                        {
                            string name = charData.NameString;
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                if (AddMemberWithoutSave(lsTargetKey, name)) importedCount++;
                            }
                        }
                    }

                    var cwlsMemberProxy = (InfoProxyCommonList*)infoModule->GetInfoProxyById(InfoProxyId.CrossWorldLinkshellMember);
                    if (cwlsMemberProxy != null && cwlsMemberProxy->EntryCount > 0)
                    {
                        var entries = cwlsMemberProxy->CharDataSpan;
                        foreach (ref readonly var charData in entries)
                        {
                            string name = charData.NameString;
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                if (AddMemberWithoutSave(cwlsTargetKey, name)) importedCount++;
                            }
                        }
                    }

                    var lsProxy = (InfoProxyLinkshell*)infoModule->GetInfoProxyById(InfoProxyId.Linkshell);
                    if (lsProxy != null)
                    {
                        var shells = lsProxy->LinkShells;
                        for (int i = 0; i < shells.Length; i++)
                        {
                            var namePtr = lsProxy->GetLinkshellName((ulong)i);
                            if (namePtr.Value != null)
                            {
                                string lsName = Marshal.PtrToStringUTF8((IntPtr)namePtr.Value) ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(lsName))
                                {
                                    var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Type == LinkshellType.StandardLinkshell && c.ChannelNumber == i + 1);
                                    if (channel != null)
                                    {
                                        channel.Label = lsName;
                                    }
                                }
                            }
                        }
                    }

                    var cwlsProxy = (InfoProxyCrossWorldLinkshell*)infoModule->GetInfoProxyById(InfoProxyId.CrossWorldLinkshell);
                    if (cwlsProxy != null)
                    {
                        var cwlsList = cwlsProxy->CrossWorldLinkshells;
                        for (int i = 0; i < cwlsList.Length; i++)
                        {
                            string cwName = cwlsList[i].Name.ToString();
                            if (!string.IsNullOrWhiteSpace(cwName))
                            {
                                var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Type == LinkshellType.CrossWorldLinkshell && c.ChannelNumber == i + 1);
                                if (channel != null)
                                {
                                    channel.Label = cwName;
                                }
                            }
                        }
                    }
                }

                configuration.Save();
                LastScanTime = DateTime.Now;
                RebuildLookupCache();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "LinkshellManager: Error during memory scan.");
                return false;
            }
        }

        private bool AddMemberWithoutSave(string channelKey, string playerName)
        {
            var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase));
            if (channel == null || string.IsNullOrWhiteSpace(playerName)) return false;

            return channel.Members.Add(playerName.Trim());
        }

        public bool AddMember(string channelKey, string playerName)
        {
            var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase));
            if (channel == null || string.IsNullOrWhiteSpace(playerName)) return false;

            string cleanName = playerName.Trim();
            if (channel.Members.Add(cleanName))
            {
                configuration.Save();
                RebuildLookupCache();
                return true;
            }

            return false;
        }

        public bool RemoveMember(string channelKey, string playerName)
        {
            var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase));
            if (channel == null || string.IsNullOrWhiteSpace(playerName)) return false;

            if (channel.Members.Remove(playerName.Trim()))
            {
                configuration.Save();
                RebuildLookupCache();
                return true;
            }

            return false;
        }

        public void ClearChannelMembers(string channelKey)
        {
            var channel = configuration.LinkshellChannels.FirstOrDefault(c => c.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
            {
                channel.Members.Clear();
                configuration.Save();
                RebuildLookupCache();
            }
        }
    }
}
