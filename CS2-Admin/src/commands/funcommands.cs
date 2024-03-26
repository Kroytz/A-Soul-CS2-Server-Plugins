using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace Admin;
public partial class Admin : BasePlugin
{
    [ConsoleCommand("css_freeze")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands> <time>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Freeze(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        if (!float.TryParse(command.GetArg(2), out float value) || value <= 0.0)
        {
            value = -1.0f;
        }

        foreach (CBasePlayerPawn? targetPawn in players.Select(p => p.Pawn.Value))
        {
            if (targetPawn == null)
            {
                continue;
            }

            targetPawn.Freeze();

            if (value > 0.0)
            {
                AddTimer(value, () =>
                {
                    targetPawn.UnFreeze();
                });
            }
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_freeze<player>", GetPlayerNameOrConsole(player), targetname, value);
        }
        else
        {
            PrintToChatAll("css_freeze<multiple>", GetPlayerNameOrConsole(player), targetname, value);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_freeze <{command.GetArg(1)}> <{value}>");
    }

    [ConsoleCommand("css_unfreeze")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_UnFreeze(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CBasePlayerPawn? targetPawn in players.Select(p => p.Pawn.Value))
        {
            if (targetPawn == null)
            {
                continue;
            }

            targetPawn.UnFreeze();
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_unfreeze<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_unfreeze<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_unfreeze {command.GetArg(1)}");
    }

    [ConsoleCommand("css_gravity")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<gravity>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Gravity(CCSPlayerController? player, CommandInfo command)
    {
        if (command.ArgCount < 1)
        {
            return;
        }

        if (!int.TryParse(command.GetArg(1), out int value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        ConVar? cvar = ConVar.Find("sv_gravity");

        if (cvar == null)
        {
            command.ReplyToCommand(Localizer["Cvar is not found", "sv_gravity"]);
            return;
        }

        Server.ExecuteCommand($"sv_gravity {value}");

        PrintToChatAll("css_cvar", GetPlayerNameOrConsole(player), "sv_gravity", value);

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_gravity <{value}>");
    }

    [ConsoleCommand("css_revive")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Revive(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, false, MultipleFlags.NORMAL);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            target.Respawn();
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_respawn<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_respawn<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_respawn <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_respawn")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Respawn(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, false, MultipleFlags.IGNORE_ALIVE_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            target.Respawn();
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_respawn<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_respawn<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_respawn <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_noclip")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands> <value>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Noclip(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, false, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        bool succeed = int.TryParse(command.GetArg(2), out int value);

        if (succeed)
        {
            value = Math.Max(0, Math.Min(1, value));

            bool noclip = Convert.ToBoolean(value);

            foreach (CBasePlayerPawn? targetPawn in players.Select(p => p.Pawn.Value))
            {
                if (targetPawn == null)
                {
                    continue;
                }

                targetPawn.Noclip(noclip);
            }

            if (players.Count == 1)
            {
                PrintToChatAll("css_noclip<player>", GetPlayerNameOrConsole(player), targetname, value);
            }
            else
            {
                PrintToChatAll("css_noclip<multiple>", GetPlayerNameOrConsole(player), targetname, value);
            }

            _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_noclip <{command.GetArg(1)}> <{value}>");
        }
        else
        {
            if (players.Count != 1)
            {
                command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
                return;
            }

            CBasePlayerPawn? targetPawn = players.First().Pawn.Value;

            if (targetPawn == null)
            {
                return;
            }

            value = targetPawn.MoveType == MoveType_t.MOVETYPE_NOCLIP ? 0 : 1;

            targetPawn.Noclip(Convert.ToBoolean(value));

            PrintToChatAll("css_noclip<player>", GetPlayerNameOrConsole(player), targetname, value);

            _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_noclip <{command.GetArg(1)}>");
        }
    }

    [ConsoleCommand("css_weapon")]
    [ConsoleCommand("css_give")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, "<#userid|name|all @ commands> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Weapon(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, false, false, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        string weapon = command.GetArg(2);

        if (!GlobalWeaponDictionary.TryGetValue(weapon, out CsItem weaponname))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Weapon is not exist"]);
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            target.GiveNamedItem(weaponname);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_weapon<player>", GetPlayerNameOrConsole(player), targetname, weaponname);
        }
        else
        {
            PrintToChatAll("css_weapon<multiple>", GetPlayerNameOrConsole(player), targetname, weaponname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_weapon <{command.GetArg(1)}> <{weaponname}>");
    }

    [ConsoleCommand("css_strip")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Strip(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            target.RemoveWeapons();

            if (Config.GiveKnifeAfterStrip)
            {
                target.GiveNamedItem(CsItem.Knife);
            }
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_strip<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_strip<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_strip <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_hp")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, "<#userid|name|all @ commands> <health>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Hp(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        if (value <= 0)
        {
            command.ReplyToCommand(Localizer["Must be higher than zero"]);
            return;
        }

        if (Config.SetHpMax100 && value > 100)
        {
            value = 100;
        }

        foreach (CCSPlayerController target in players)
        {
            target.Health(value);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_hp<player>", GetPlayerNameOrConsole(player), targetname, value);
        }
        else
        {
            PrintToChatAll("css_hp<multiple>", GetPlayerNameOrConsole(player), targetname, value);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_hp <{command.GetArg(1)}> <{value}>");
    }

    [ConsoleCommand("css_sethp")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, "<team> <health> - Sets team players' spawn health", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_SetHp(CCSPlayerController? player, CommandInfo command)
    {
        string arg = command.GetArg(1);

        CsTeam team = arg switch
        {
            string s when s.StartsWith("T") => CsTeam.Terrorist,
            string s when s.StartsWith("C") => CsTeam.CounterTerrorist,
            "2" => CsTeam.Terrorist,
            "3" => CsTeam.CounterTerrorist,
            _ => CsTeam.None
        };

        if (team == CsTeam.None)
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["No team exists"]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        if (value <= 0)
        {
            command.ReplyToCommand(Localizer["Must be higher than zero"]);
            return;
        }

        if (team == CsTeam.CounterTerrorist)
        {
            Config.CTDefaultHealth = value;
        }
        else
        {
            Config.TDefaultHealth = value;
        }

        PrintToChatAll("css_sethp", GetPlayerNameOrConsole(player), team, value);

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_sethp <{team}> <{value}>");
    }

    [ConsoleCommand("css_speed")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, "<#userid|name|all @ commands> <value>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Speed(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        if (!float.TryParse(command.GetArg(2), out float value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        foreach (CCSPlayerPawn? targetPlayerPawn in players.Select(p => p.PlayerPawn.Value))
        {
            if (targetPlayerPawn == null)
            {
                continue;
            }

            targetPlayerPawn.Speed(value);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_speed<player>", GetPlayerNameOrConsole(player), targetname, value);
        }
        else
        {
            PrintToChatAll("css_speed<multiple>", GetPlayerNameOrConsole(player), targetname, value);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_speed <{command.GetArg(1)}> <{value}>");
    }

    [ConsoleCommand("css_god")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, "<#userid|name|all @ commands> <value>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_God(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        value = Math.Max(0, Math.Min(1, value));

        bool godmode = Convert.ToBoolean(value);

        foreach (CCSPlayerPawn? targetPlayerPawn in players.Select(p => p.PlayerPawn.Value))
        {
            if (targetPlayerPawn == null)
            {
                continue;
            }

            targetPlayerPawn.Godmode(godmode);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_god<player>", GetPlayerNameOrConsole(player), targetname, value);
        }
        else
        {
            PrintToChatAll("css_god<multiple>", GetPlayerNameOrConsole(player), targetname, value);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_god <{command.GetArg(1)}> <{value}>");
    }

    [ConsoleCommand("css_team")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 2, "<#userid|name|all @ commands> <value>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Team(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, false, false, MultipleFlags.NORMAL);

        if (players.Count == 0)
        {
            return;
        }

        string teamarg = command.GetArg(2).ToLower();
        string teamname;
        CsTeam team;

        switch (teamarg[0])
        {
            case 'c':
                {
                    teamname = "CT";
                    team = CsTeam.CounterTerrorist;
                    break;
                }
            case '2':
                {
                    teamname = "CT";
                    team = CsTeam.CounterTerrorist;
                    break;
                }
            case 't':
                {
                    teamname = "T";
                    team = CsTeam.Terrorist;
                    break;
                }
            case '1':
                {
                    teamname = "T";
                    team = CsTeam.Terrorist;
                    break;
                }
            default:
                {
                    teamname = "SPEC";
                    team = CsTeam.Spectator;
                    break;
                }
        }

        foreach (CCSPlayerController target in players)
        {
            target.ChangeTeam(team);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_team<player>", GetPlayerNameOrConsole(player), targetname, teamname);
        }
        else
        {
            PrintToChatAll("css_team<multiple>", GetPlayerNameOrConsole(player), targetname, teamname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_team <{command.GetArg(1)}> <{teamname}>");
    }

    [ConsoleCommand("css_swap")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, "<#userid|name>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Swap(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 2, true, false, MultipleFlags.NORMAL);

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerController target = players.Single();

        string teamname;
        CsTeam team;

        if (target.Team == CsTeam.Terrorist)
        {
            teamname = "CT";
            team = CsTeam.CounterTerrorist;
        }
        else
        {
            teamname = "T";
            team = CsTeam.Terrorist;
        }

        target.SwitchTeam(team);

        if (players.Count == 1)
        {
            PrintToChatAll("css_team<player>", GetPlayerNameOrConsole(player), targetname, teamname);
        }
        else
        {
            PrintToChatAll("css_team<multiple>", GetPlayerNameOrConsole(player), targetname, teamname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_swap <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_bury")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Bury(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CBasePlayerPawn? targetPawn in players.Select(p => p.Pawn.Value))
        {
            if (targetPawn == null)
            {
                continue;
            }

            targetPawn.Bury();
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_bury<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_bury<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_bury <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_unbury")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_UnBury(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        foreach (CBasePlayerPawn? targetPawn in players.Select(p => p.Pawn.Value))
        {
            if (targetPawn?.AbsOrigin == null || targetPawn.AbsRotation == null)
            {
                continue;
            }

            targetPawn.UnBury();
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_unbury<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_unbury<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_unbury <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_clean")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 0, "- Clean weapons on the ground", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Clean(CCSPlayerController? player, CommandInfo command)
    {
        RemoveWeaponsOnTheGround();

        PrintToChatAll("css_clean", GetPlayerNameOrConsole(player));

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_clean");
    }

    [ConsoleCommand("css_goto")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name> - Teleport player to a player's position", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Command_Goto(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, true, false, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerController target = players.Single();

        CCSPlayerPawn? targetPlayerPawn = target.PlayerPawn.Value;
        CCSPlayerPawn? playerPlayerPawn = player.PlayerPawn.Value;

        if (targetPlayerPawn == null || playerPlayerPawn == null)
        {
            return;
        }

        playerPlayerPawn.TeleportToPlayer(targetPlayerPawn);

        PrintToChatAll("css_goto", player.PlayerName, targetname);

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_goto <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_bring")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands> - Teleport players to a player's position", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Command_Bring(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerPawn? playerPlayerPawn = player.PlayerPawn.Value;

        if (playerPlayerPawn == null)
        {
            return;
        }

        foreach (CCSPlayerPawn? targetPlayerPawn in players.Select(p => p.PlayerPawn.Value))
        {
            if (targetPlayerPawn == null)
            {
                continue;
            }

            targetPlayerPawn.TeleportToPlayer(playerPlayerPawn);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_bring<player>", GetPlayerNameOrConsole(player), targetname);
        }
        else
        {
            PrintToChatAll("css_bring<multiple>", GetPlayerNameOrConsole(player), targetname);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_bring <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_hrespawn")]
    [ConsoleCommand("css_1up")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name> - Respawns a player in his last known death position.", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_HRespawn(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, true, false, MultipleFlags.IGNORE_ALIVE_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        CCSPlayerController target = players.Single();

        CCSPlayerPawn? targetPawn = target.PlayerPawn.Value;

        if (targetPawn == null || targetPawn.AbsRotation == null)
        {
            return;
        }

        Vector position = GlobalHRespawnPlayers.First(p => p.Key == target).Value;

        target.Respawn();
        targetPawn.Teleport(position, targetPawn.AbsRotation, targetPawn.AbsVelocity);

        PrintToChatAll("css_hrespawn", GetPlayerNameOrConsole(player), targetname);

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_hrespawn <{command.GetArg(1)}>");
    }

    [ConsoleCommand("css_glow")]
    [ConsoleCommand("css_color")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands> <color>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Command_Glow(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, false, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        Color color = Color.White;

        string colorstring = command.GetArg(2);

        if (!string.IsNullOrEmpty(colorstring))
        {
            if (!Enum.TryParse(colorstring, true, out KnownColor knownColor))
            {
                command.ReplyToCommand(Localizer["Prefix"] + Localizer["No color exists"]);
                return;
            }

            color = Color.FromKnownColor(knownColor);
        }

        foreach (CCSPlayerPawn? targetPlayerPawn in players.Select(p => p.PlayerPawn.Value))
        {
            if (targetPlayerPawn == null)
            {
                continue;
            }

            targetPlayerPawn.Color(color);
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_glow<player>", GetPlayerNameOrConsole(player), targetname, Localizer[color.Name]);
        }
        else
        {
            PrintToChatAll("css_glow<multiple>", GetPlayerNameOrConsole(player), targetname, Localizer[color.Name]);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_glow <{command.GetArg(1)} <{color}>");
    }

    [ConsoleCommand("css_beacon")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, "<#userid|name|all @ commands> <value>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void Command_Beacon(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, command, 1, false, true, MultipleFlags.IGNORE_DEAD_PLAYERS);

        if (players.Count == 0)
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        if (value > 0)
        {
            foreach (CCSPlayerController target in players)
            {
                if (!GlobalBeaconTimer.ContainsKey(target))
                {
                    CounterStrikeSharp.API.Modules.Timers.Timer timer = AddTimer(3.0f, () => target.Circle(), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
                    GlobalBeaconTimer[target] = timer;
                }
            }
        }
        else
        {
            foreach (CCSPlayerController target in players.ToList())
            {
                if (GlobalBeaconTimer.TryGetValue(target, out CounterStrikeSharp.API.Modules.Timers.Timer? timer))
                {
                    timer.Kill();
                    GlobalBeaconTimer.Remove(target);
                }
            }
        }

        if (players.Count == 1)
        {
            PrintToChatAll("css_beacon<player>", GetPlayerNameOrConsole(player), targetname, value);
        }
        else
        {
            PrintToChatAll("css_beacon<multiple>", GetPlayerNameOrConsole(player), targetname, value);
        }

        _ = SendDiscordMessage($"[{GetPlayerSteamIdOrConsole(player)}] {GetPlayerNameOrConsole(player)} -> css_beacon <{command.GetArg(1)}> <{value}>");
    }
}