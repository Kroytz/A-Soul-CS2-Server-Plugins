using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;

namespace Admin;

public partial class Admin : BasePlugin
{
    [ConsoleCommand("css_ban")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 2, "<#userid|name> <time> <reason>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Ban(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, true, true, MultipleFlags.NORMAL);

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerController target = players.Single();

        if (!AdminManager.CanPlayerTarget(player, target))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["You cannot target"]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int time))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        string reason = command.GetArg(3) ?? Localizer["Unknown"];

        SetPunishmentForPlayer(player, target, "ban", reason, time, true);

        KickPlayer(target, reason);

        PrintToChatAll("css_ban", GetPlayerNameOrConsole(player), targetname, time, reason);
        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_ban <{targetname}> <{time}> <{reason}>");
    }

    [ConsoleCommand("css_unban")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_UnBan(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 1)
        {
            return;
        }

        string steamid = command.GetArg(1);

        if (!SteamID.TryParse(steamid, out SteamID? steamId) || steamId == null)
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be a steamid"]);
            return;
        }

        RemovePunishment(steamId.SteamId64, "ban", true);

        PrintToChatAll("css_unban", GetPlayerNameOrConsole(player), steamid);
        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_unban <{steamid}>");
    }

    [ConsoleCommand("css_addban")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 2, "<steamid> <time> <reason>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Addban(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 2)
        {
            return;
        }

        string steamid = command.GetArg(1);

        if (!SteamID.TryParse(steamid, out SteamID? steamId) || steamId == null)
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be a steamid"]);
            return;
        }

        if (AdminManager.GetPlayerImmunity(player) < AdminManager.GetPlayerImmunity(steamId))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["You cannot target"]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int time))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        string reason = command.GetArg(3) ?? Localizer["Unknown"];

        CCSPlayerController? target = Utilities.GetPlayerFromSteamId(steamId.SteamId64);

        if (target != null && target.Valid())
        {
            SetPunishmentForPlayer(player, target, "ban", reason, time, true);

            KickPlayer(target, reason);

            PrintToChatAll("css_ban", GetPlayerNameOrConsole(player), target.PlayerName, time, reason);
            _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_addban <{target.PlayerName}> <{time}> <{reason}>");
        }
        else
        {
            AddPunishmentForPlayer(player, steamId.SteamId64, reason, time, true);

            PrintToChatAll("css_addban", GetPlayerNameOrConsole(player), steamId.SteamId64, time, reason);
            _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_addban <{steamId.SteamId64}> <{time}> <{reason}>");
        }
    }
}