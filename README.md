# LinkshellNameColor

**LinkshellNameColor** is a Final Fantasy XIV [Dalamud](https://github.com/goatcorp/Dalamud) plugin that automatically recolors the nameplates of player characters matching members in your **Linkshells (LS1–8)** and **Cross-World Linkshells (CWLS1–8)**.

Inspired by the functionality of [FCNameColor](https://github.com/WesselKuipers/FCNameColor), LinkshellNameColor makes it effortless to visually identify shellmates in crowded hunt trains, major cities, housing districts, and raid hubs.

---

## ✨ Features

- 🎨 **Per-Linkshell Color Customization**: Assign distinct colors to each of your 8 standard Linkshells (`LS1`–`LS8`) and 8 Cross-World Linkshells (`CWLS1`–`CWLS8`).
- ⚡ **Sensible Onboarding Defaults**: Pre-configured out-of-the-box with vibrant, high-contrast default colors so you can start spotting friends immediately.
- 🔍 **Auto-Scan & Roster Sync**: Interacts with in-game memory proxies (`InfoProxyLinkshell` / `InfoProxyCrossWorldLinkshell`) to automatically import shellmates.
- 📝 **Manual Roster Management**: Full UI to search, add, or remove member names manually.
- 🏷️ **Linkshell Badges**: Optional prefix or suffix badges (e.g. `[LS1] Player Name` or `Player Name [CWLS3]`).
- 🎯 **Flexible Recolor Targets**: Choose whether to recolor Character Names, Titles, and/or Free Company tags.
- 🌈 **Color Palettes**: Includes quick presets (*Vibrant Neon*, *Soft Pastel*, *Classic FC*, *High Contrast*).

---

## 🚀 Quick Start

1. Open the plugin configuration window in-game using `/lsnc` or `/linkshellnamecolor`.
2. Go to the **Member Rosters** tab and click **Scan In-Game Linkshells** to automatically load your active shellmates.
3. Customize colors per Linkshell in the **Linkshell Colors** tab or apply a palette from **Presets & Onboarding**.

---

## 💬 Commands

| Command | Action |
| --- | --- |
| `/lsnc` | Opens / closes the LinkshellNameColor configuration UI |
| `/linkshellnamecolor` | Alias for `/lsnc` |
| `/lsnc scan` | Triggers an immediate memory scan of active in-game Linkshells |
| `/lsnc toggle` | Quickly enables or disables nameplate recoloring |

---

## 🛠️ Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- FFXIV client with XIVLauncher / Dalamud installed

### Build Steps
```bash
git clone https://github.com/YourUsername/LinkshellNameColor.git
cd LinkshellNameColor
dotnet build LinkshellNameColor.csproj -c Release
```

The compiled assembly will be output to `bin/Release/LinkshellNameColor.dll`.

### Installing into Dalamud (Dev Plugin)
1. Open FFXIV and launch Dalamud settings using `/dalamudsettings`.
2. Navigate to the **Experimental** tab.
3. Under **Dev Plugin Locations**, add the full path to `f:\Repos\LinkshellNameColor\bin\Release`.
4. Run `/xlplugins` in-game and enable **LinkshellNameColor**.

---

## 📂 Project Architecture

```
LinkshellNameColor/
├── LinkshellNameColor.csproj   # Build configuration (net10.0-windows / Dalamud)
├── LinkshellNameColor.json     # Dalamud Plugin Manifest
├── Plugin.cs                   # Plugin Entry Point & Service Injections
├── Configuration.cs            # Persistent Plugin Options & Onboarding Defaults
├── Models/
│   └── LinkshellConfig.cs      # Data models for Linkshell channels & member matches
├── Services/
│   ├── LinkshellManager.cs     # Linkshell member scanning & fast O(1) lookup index
│   └── NamePlateService.cs     # INamePlateGui hook for dynamic SeString nameplate recoloring
└── UI/
    └── ConfigWindow.cs         # ImGui configuration window (Tabs: Colors, Rosters, Settings, Presets)
```

---

## 📄 License
Distributed under the MIT License.
