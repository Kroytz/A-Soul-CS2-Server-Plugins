
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Entities;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using System.Numerics;
using static CounterStrikeSharp.API.Core.Listeners;
using System.Text.Json.Serialization;
using Storage;

namespace CustomWeaponSKin;

public class Model
{
    public long itemdef { get; set; }
    public int type { get; set; }
    public string name { get; set; }
    public required string path { get; set; }
}

public class ModelConfig : BasePluginConfig
{
    [JsonPropertyName("Models")] public Dictionary<string, Model> Models { get; set; } = new Dictionary<string, Model>();

    [JsonPropertyName("MenuType")] public string MenuType { get; set; } = "chat"; // chat or centerhtml

    [JsonPropertyName("StorageType")] public string StorageType { get; set; } = "sqlite";

    [JsonPropertyName("MySQL_IP")] public string MySQLIP { get; set; } = "";
    [JsonPropertyName("MySQL_Port")] public string MySQLPort { get; set; } = "";
    [JsonPropertyName("MySQL_User")] public string MySQLUser { get; set; } = "";
    [JsonPropertyName("MySQL_Password")] public string MySQLPassword { get; set; } = "";
    [JsonPropertyName("MySQL_Database")] public string MySQLDatabase { get; set; } = "";
    [JsonPropertyName("MySQL_Table")] public string MySQLTable { get; set; } = "customweaponskin";

}

[MinimumApiVersion(197)]
public partial class CustomWeaponSkin : BasePlugin, IPluginConfig<ModelConfig>
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "";
    public override string ModuleName => "CustomWeaponSkin";
    public override string ModuleVersion => "1.0.0";

    public required ModelConfig Config { get; set; }
    public IStorage? storage;

    public string PL_PREFIX = $" [{ChatColors.Green}CWS{ChatColors.Default}] ";

    Dictionary<ulong, Dictionary<long, Model>> dictSteamToItemDefModel = new Dictionary<ulong, Dictionary<long, Model>>();

    public override void Load(bool hotReload)
    {
        storage = null;
        switch (Config.StorageType)
        {
            case "sqlite":
                storage = new SqliteStorage(ModuleDirectory);
                break;
            case "mysql":
                storage = new MySQLStorage(Config.MySQLIP, Config.MySQLPort, Config.MySQLUser, Config.MySQLPassword, Config.MySQLDatabase, Config.MySQLTable);
                break;
        };
        if (storage == null)
        {
            throw new Exception("Failed to initialize storage. Please check your config");
        }

        RegisterListener<Listeners.OnServerPrecacheResources>((manifest) =>
        {
            foreach (var model in Config.Models.Values.ToList())
            {
                Server.PrintToConsole($"CustomWeaponSkin :: Precaching {model.path}");
                manifest.AddResource(model.path);
            }
        });

        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventItemEquip>(OnItemEquip);
    }

    [ConsoleCommand("css_cwc", "Clear skin")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnClearSkinCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player is null)
        {
            return;
        }

        var steam64 = player.SteamID;
        dictSteamToItemDefModel[steam64].Clear();
        storage!.ClearPlayerAllModelAsync(steam64);
        player.PrintToChat(PL_PREFIX + "已清除所有装备的皮肤.");
    }

    [ConsoleCommand("css_cws", "Set player custom weapon skin")]
    [CommandHelper(minArgs: 0, usage: "[index]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player is null)
        {
            return;
        }

        int idx = 0;
        if (commandInfo.ArgCount > 1)
        {
            var _idx = commandInfo.GetArg(1);
            idx = Int32.Parse(_idx);
        }

        var list = Config.Models.Values.ToList();
        if (idx > list.Count || idx <= 0)
        {
            player?.PrintToChat(PL_PREFIX + "指令用法: .cws <索引>, 清空皮肤 .cwc, 当前可用索引如下: ");
            var i = 1;
            string printstr = "";
            foreach (var model in list)
            {
                printstr += $" {ChatColors.Yellow}{i}{ChatColors.Default} - {ChatColors.Yellow}{model.name}{ChatColors.Default} ";
                if (i % 3 == 0)
                {
                    player?.PrintToChat(printstr);
                    printstr = "";
                }
                else
                {
                    printstr += $" ◆ ";
                }

                i++;
            }
            return;
        }

        if (idx > 0)
        {
            idx -= 1;

            Model mod = list[idx];

            var steam64 = player.SteamID;
            if (!dictSteamToItemDefModel.ContainsKey(steam64))
            {
                dictSteamToItemDefModel[steam64] = new Dictionary<long, Model>();
            }
            dictSteamToItemDefModel[steam64][mod.itemdef] = mod;

            var saveKey = "";
            foreach (var dv in Config.Models)
            {
                if (dv.Value.name == mod.name)
                {
                    saveKey = dv.Key;
                }
            }

            storage!.SetPlayerModel(steam64, mod.itemdef, saveKey);
            player.PrintToChat(PL_PREFIX + $"已装配皮肤 {mod.name} ");
        }
    }

    public void OnConfigParsed(ModelConfig config)
    {
        var availableStorageType = new[] { "sqlite", "mysql" };
        if (!availableStorageType.Contains(config.StorageType))
        {
            throw new Exception($"Unknown storage type: {Config.StorageType}, available types: {string.Join(",", availableStorageType)}");
        }

        if (config.StorageType == "mysql")
        {
            if (config.MySQLIP == "")
            {
                throw new Exception("You must fill in the MySQL_IP");
            }
            if (config.MySQLPort == "")
            {
                throw new Exception("You must fill in the MYSQL_Port");
            }
            if (config.MySQLUser == "")
            {
                throw new Exception("You must fill in the MYSQL_User");
            }
            if (config.MySQLPassword == "")
            {
                throw new Exception("You must fill in the MYSQL_Password");
            }
            if (config.MySQLDatabase == "")
            {
                throw new Exception("You must fill in the MySQL_Database");
            }
        }

        if (config.MenuType.ToLower() != "chat" && config.MenuType.ToLower() != "centerhtml")
        {
            throw new Exception($"Unknown menu type: {config.MenuType}");
        }
        config.MenuType = config.MenuType.ToLower();
        //foreach (var entry in config.Models)
        //{
        //    ModelService.InitializeModel(entry.Key, entry.Value);
        //}

        Config = config;
    }

    public async void RefreshPlayerInventory(CCSPlayerController player)
    {
        var steam64 = player.SteamID;
        var settings = await storage!.GetPlayerAllModelAsync(steam64);
        Server.PrintToConsole($"RefreshPlayerInventory() -> Done model cache for {steam64}, size = {settings.Count}");
        if (settings.Count == 0)
        {
            return;
        }

        var modelList = Config.Models.Values.ToList();
        if (modelList.Count == 0)
        {
            return;
        }

        if (!dictSteamToItemDefModel.ContainsKey(steam64))
        {
            dictSteamToItemDefModel[steam64] = new Dictionary<long, Model>();
        }

        var modelDict = Config.Models;
        foreach (var key in settings)
        {
            foreach (var model in modelDict)
            {
                if (model.Key == key)
                {
                    Server.PrintToConsole($"RefreshPlayerInventory() -> Found model {key} for {steam64}");
                    dictSteamToItemDefModel[steam64][model.Value.itemdef] = model.Value;
                    break;
                }
            }
        }
    }

    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            RefreshPlayerInventory(player);
        }

        return HookResult.Continue;
    }

    public HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        //Server.PrintToConsole($"OnItemEquip triggered, player {player.Index}");
        if (player == null || player.IsBot)
            return HookResult.Continue;

        //Server.PrintToConsole($"{player.Index} pawn Check");
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        var steam64 = player.SteamID;
        if (!dictSteamToItemDefModel.ContainsKey(steam64))
            return HookResult.Continue;

        //Server.PrintToConsole($"{player.Index} ActiveWeapon Check");
        CBasePlayerWeapon? weapon = pawn.WeaponServices?.ActiveWeapon.Value;
        if (weapon == null || !weapon.IsValid)
            return HookResult.Continue;

        //Server.PrintToConsole($"{player.Index} GetVm0");
        CBaseViewModel? vm = GetPlayerViewModel(player);
        if (vm == null || !vm.IsValid)
            return HookResult.Continue;

        string name = @event.Item;
        long itemdef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        CSWeaponType type = (CSWeaponType)@event.Weptype;
        Server.PrintToConsole($"{player.Index} DefIdx = {itemdef}, Name = {name}, type = {type}");

        if (name == "knife")
        {
            itemdef = 0;
        }

        if (dictSteamToItemDefModel[steam64].ContainsKey(itemdef))
        {
            Model mod = dictSteamToItemDefModel[steam64][itemdef];
            //Server.PrintToConsole($"{player.Index} Found model for {itemdef} - {mod.name}");
            vm.SetModel(mod.path);
            weapon.SetModel(mod.path);
        }
        else
        {
            var node = weapon.CBodyComponent?.SceneNode;
            if (node == null)
            {
                return HookResult.Continue;
            }

            var skeleton = GetSkeletonInstance(node);
            var modelname = skeleton.ModelState.ModelName;
            Server.PrintToConsole($"{player.Index} Item model name is {modelname}");
            vm.SetModel(modelname);
        }

        return HookResult.Continue;
    }

    private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
    {
        Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
        return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
    }

    // This is a hack by KillStr3aK.
    public unsafe CBaseViewModel? GetPlayerViewModel(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var references = MemoryMarshal.CreateSpan(ref ptr, 3);
        var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
        if (viewModel == null || viewModel.Value == null) return null;
        return viewModel.Value;
    }

    public bool IsPlayerValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsHLTV;
    }

    public bool IsPlayerHumanAndValid(CCSPlayerController? player)
    {
        return IsPlayerValid(player) && !player!.IsBot;
    }
}
