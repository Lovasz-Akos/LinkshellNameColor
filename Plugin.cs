using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using LinkshellNameColor.Services;
using LinkshellNameColor.UI;

namespace LinkshellNameColor
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "LinkshellNameColor";

        private const string CommandName = "/lsnc";
        private const string AltCommandName = "/linkshellnamecolor";

        private readonly IDalamudPluginInterface pluginInterface;
        private readonly ICommandManager commandManager;
        private readonly INamePlateGui namePlateGui;
        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly IPluginLog log;
        private readonly IObjectTable objectTable;
        private readonly IChatGui chatGui;

        public Configuration Configuration { get; private set; }
        public LinkshellManager LinkshellManager { get; private set; }
        public NamePlateService NamePlateService { get; private set; }

        public WindowSystem WindowSystem { get; private set; } = new("LinkshellNameColor");
        public ConfigWindow ConfigWindow { get; private set; }

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            INamePlateGui namePlateGui,
            IClientState clientState,
            IFramework framework,
            IPluginLog log,
            IObjectTable objectTable,
            IChatGui chatGui)
        {
            this.pluginInterface = pluginInterface;
            this.commandManager = commandManager;
            this.namePlateGui = namePlateGui;
            this.clientState = clientState;
            this.framework = framework;
            this.log = log;
            this.objectTable = objectTable;
            this.chatGui = chatGui;

            Configuration = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this.pluginInterface);

            LinkshellManager = new LinkshellManager(Configuration, this.log, this.clientState, this.objectTable);
            NamePlateService = new NamePlateService(this.namePlateGui, Configuration, LinkshellManager, this.log, this.chatGui);

            ConfigWindow = new ConfigWindow(Configuration, LinkshellManager, NamePlateService);
            WindowSystem.AddWindow(ConfigWindow);

            this.pluginInterface.UiBuilder.Draw += DrawUI;
            this.pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            this.clientState.Login += OnLogin;

            this.commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open configuration window. Commands: '/lsnc scan', '/lsnc debug', '/lsnc toggle'."
            });

            this.commandManager.AddHandler(AltCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Alias for /lsnc."
            });

            if (this.clientState.IsLoggedIn)
            {
                this.LinkshellManager.ScanInGameLinkshells();
                this.NamePlateService.RequestRedraw();
            }

            this.log.Info("LinkshellNameColor initialized.");
        }

        private void OnLogin()
        {
            LinkshellManager.ScanInGameLinkshells();
            NamePlateService.RequestRedraw();
        }

        private void OnCommand(string command, string args)
        {
            string cleanArgs = args.Trim().ToLowerInvariant();

            if (cleanArgs == "scan")
            {
                bool success = LinkshellManager.ScanInGameLinkshells();
                NamePlateService.RequestRedraw();
                log.Info(success ? "Linkshell scan completed." : "Linkshell scan failed or not logged in.");
            }
            else if (cleanArgs == "debug")
            {
                NamePlateService.TriggerDebugDump();
            }
            else if (cleanArgs == "toggle")
            {
                Configuration.PluginEnabled = !Configuration.PluginEnabled;
                Configuration.Save();
                NamePlateService.RequestRedraw();
                log.Info($"LinkshellNameColor is now {(Configuration.PluginEnabled ? "ENABLED" : "DISABLED")}.");
            }
            else
            {
                ToggleConfigUI();
            }
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void ToggleConfigUI()
        {
            ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
        }

        public void Dispose()
        {
            this.clientState.Login -= OnLogin;
            this.commandManager.RemoveHandler(CommandName);
            this.commandManager.RemoveHandler(AltCommandName);

            this.pluginInterface.UiBuilder.Draw -= DrawUI;
            this.pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            NamePlateService.Dispose();

            this.log.Info("LinkshellNameColor disposed.");
        }
    }
}
