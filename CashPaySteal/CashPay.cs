using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;

using System.Numerics;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;

namespace cashpay;

public class CashPay : BasePlugin
{
    public override string ModuleName => "CashPay";
    public override string ModuleVersion => "0.0.2";
    public override string ModuleAuthor => "7ychu5";
    public override string ModuleDescription => "Pay your game cash to the other player";

    public static bool PayToggle = true;

    public static int PaySteal = 0;

    //public static bool PayLimit = true;
    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerPing>((@event, info) =>
        {
            Vector3 origin;
            origin.X = @event.X;
            origin.Y = @event.Y;
            origin.Z = @event.Z;

            double min_distance = 64.0;

            var host = @event.Userid;
            if (host.PlayerPawn.Value is null) return HookResult.Continue;

            CCSPlayerController? victim = null;

            var playerEntities = Utilities.GetPlayers().Where(players => players.Team >= CsTeam.Terrorist && players.Connected == PlayerConnectedState.PlayerConnected && players.IsValid).ToList();

            foreach (var player in playerEntities)
            {
                if (player == host) continue;
                if (player.PlayerPawn == null || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) continue;
                var pawn = player.PlayerPawn.Value;
                Vector? ply_origin = pawn.AbsOrigin;
                if (ply_origin is null) continue;

                Vector3 v3_origin;
                v3_origin.X = ply_origin.X;
                v3_origin.Y = ply_origin.Y;
                v3_origin.Z = ply_origin.Z;

                if (Distance3D(v3_origin, origin) <= min_distance)
                {
                    min_distance = Distance3D(v3_origin, origin);
                    victim = player;
                }
            }

            if (victim is null || victim.PlayerPawn.Value is null || !victim.PlayerPawn.IsValid) return HookResult.Continue;

            if (host.PlayerPawn.Value.TeamNum != victim.PlayerPawn.Value.TeamNum)
            {
                if (PaySteal == 0) Pay(host, victim, -50);
                else if (PaySteal == 1)
                {
                    if (victim.PlayerPawn.Value.Health > 0) Pay(host, victim, -50);
                }
                else if (PaySteal == 2)
                {
                    if (victim.PlayerPawn.Value.Health <= 0) Pay(host, victim, -50);
                }
            }
            else
            {
                Pay(host, victim, 100);
            }

            return HookResult.Continue;
        });
    }

    public enum MultipleFlags
    {
        NORMAL = 0,
        IGNORE_DEAD_PLAYERS,
        IGNORE_ALIVE_PLAYERS
    }

    private static readonly Dictionary<string, TargetType> TargetTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "@all", TargetType.GroupAll },
        { "@bots", TargetType.GroupBots },
        { "@human", TargetType.GroupHumans },
        { "@alive", TargetType.GroupAlive },
        { "@dead", TargetType.GroupDead },
        { "@!me", TargetType.GroupNotMe },
        { "@me", TargetType.PlayerMe },
        { "@ct", TargetType.TeamCt },
        { "@t", TargetType.TeamT },
        { "@spec", TargetType.TeamSpec }
    };

    private (List<CCSPlayerController> players, string targetname) FindTarget
    (
        CCSPlayerController? player,
        CommandInfo command,
        int minArgCount,
        bool singletarget,
        bool immunitycheck,
        MultipleFlags flags
    )
    {
        if (command.ArgCount < minArgCount)
        {
            return (new List<CCSPlayerController>(), string.Empty);
        }

        TargetResult targetresult = command.GetArgTargetResult(1);

        if (targetresult.Players.Count == 0)
        {
            command.ReplyToCommand("No matching client");
            return (new List<CCSPlayerController>(), string.Empty);
        }
        else if (singletarget && targetresult.Players.Count > 1)
        {
            command.ReplyToCommand("More than one client matched");
            return (new List<CCSPlayerController>(), string.Empty);
        }

        if (immunitycheck)
        {
            targetresult.Players.RemoveAll(target => !AdminManager.CanPlayerTarget(player, target));

            if (targetresult.Players.Count == 0)
            {
                command.ReplyToCommand("You cannot target");
                return (new List<CCSPlayerController>(), string.Empty);
            }
        }

        if (flags == MultipleFlags.IGNORE_DEAD_PLAYERS)
        {
            targetresult.Players.RemoveAll(target => !target.PawnIsAlive);

            if (targetresult.Players.Count == 0)
            {
                command.ReplyToCommand("You can target only alive players");
                return (new List<CCSPlayerController>(), string.Empty);
            }
        }
        else if (flags == MultipleFlags.IGNORE_ALIVE_PLAYERS)
        {
            targetresult.Players.RemoveAll(target => target.PawnIsAlive);

            if (targetresult.Players.Count == 0)
            {
                command.ReplyToCommand("You can target only dead players");
                return (new List<CCSPlayerController>(), string.Empty);
            }
        }

        string targetname;

        if (targetresult.Players.Count == 1)
        {
            targetname = targetresult.Players.Single().PlayerName;
        }
        else
        {
            TargetTypeMap.TryGetValue(command.GetArg(1), out TargetType type);

            targetname = type switch
            {
                TargetType.GroupAll => "all",
                TargetType.GroupBots => "bots",
                TargetType.GroupHumans => "humans",
                TargetType.GroupAlive => "alive",
                TargetType.GroupDead => "dead",
                TargetType.GroupNotMe => "notme",
                TargetType.PlayerMe => targetresult.Players.First().PlayerName,
                TargetType.TeamCt => "ct",
                TargetType.TeamT => "t",
                TargetType.TeamSpec => "spec",
                _ => targetresult.Players.First().PlayerName
            };
        }

        return (targetresult.Players, targetname);
    }

    [ConsoleCommand("css_pay", "Pay Cash to sb.")]
    [CommandHelper(minArgs: 2, usage: "[userid],[cashnum]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPay(CCSPlayerController? player, CommandInfo commandInfo)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget(player, commandInfo, 1, false, false, MultipleFlags.NORMAL);

        if (players.Count == 0)
        {
            return;
        }

        var _cashnum = commandInfo.GetArg(2);
        int cashnum = Int32.Parse(_cashnum);

        foreach (CCSPlayerController victim in players)
        {
            if (player == null
            || victim == null
            || player.InGameMoneyServices == null
            || victim.InGameMoneyServices == null
            || player.PlayerPawn.Value == null
            || !player.PlayerPawn.Value.IsValid
            || player.PlayerPawn.Value.Health <= 0
            || victim.PlayerPawn.Value == null
            || !victim.PlayerPawn.Value.IsValid
            || victim.PlayerPawn.Value.Health <= 0) return;

            if (cashnum <= 0)
            {
                player.ExecuteClientCommand($"play sounds/ui/armsrace_level_down.vsnd");
                player.PrintToCenter("Illegal Cash number");
                return;
            }

            Pay(player, victim, cashnum);

            return;
        }
    }

    [ConsoleCommand("css_pay_force", "force sb. Pay Cash to sb.")]
    [RequiresPermissions("@css/admin")]
    [CommandHelper(minArgs: 2, usage: "[userid1],[userid2],[cashnum]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPayForce(CCSPlayerController? player, CommandInfo commandInfo)
    {
        (List<CCSPlayerController> givers, string givername) = FindTarget(player, commandInfo, 1, false, false, MultipleFlags.NORMAL);
        (List<CCSPlayerController> receivers, string receivername) = FindTarget(player, commandInfo, 2, false, false, MultipleFlags.NORMAL);

        if (givers.Count == 0 || receivers.Count == 0)
        {
            return;
        }

        var _cashnum = commandInfo.GetArg(3);
        int cashnum = Int32.Parse(_cashnum);

        foreach (CCSPlayerController giver in givers)
        {
            foreach (CCSPlayerController victim in receivers)
            {
                if (giver == null
                || victim == null
                || giver.InGameMoneyServices == null
                || victim.InGameMoneyServices == null
                || giver.PlayerPawn.Value == null
                || !giver.PlayerPawn.Value.IsValid
                || giver.PlayerPawn.Value.Health <= 0
                || victim.PlayerPawn.Value == null
                || !victim.PlayerPawn.Value.IsValid
                || victim.PlayerPawn.Value.Health <= 0) return;

                Pay(giver, victim, cashnum);
            }

        }

        return;
    }


    [ConsoleCommand("css_pay_toggle", "Toggle the switch of the plugin")]
    [RequiresPermissions("@css/admin")]
    //[CommandHelper(minArgs: 0, usage: "[toggle]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPayToggle(CCSPlayerController? player, CommandInfo commandInfo)
    {
        PayToggle = !PayToggle;
        if (PayToggle) Server.PrintToChatAll(ModuleName + "CashPay Plugin TurnON");
        else Server.PrintToChatAll(ModuleName + "CashPay Plugin TurnOff");
        return;
    }

    [ConsoleCommand("css_pay_steal", "Toggle the steal switch of the plugin")]
    [RequiresPermissions("@css/admin")]
    [CommandHelper(minArgs: 1, usage: "[toggle]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPayStealSwitch(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var temp = commandInfo.ArgByIndex(1);
        PaySteal = Int32.Parse(temp);
        if (PaySteal < 0 || PaySteal > 2) PaySteal = 0;
        switch (PaySteal)
        {
            case 0: Server.PrintToChatAll(ModuleName + " You can now steal EVERYONE."); break;
            case 1: Server.PrintToChatAll(ModuleName + " You can now steal only ALIVE PLAYERS."); break;
            case 2: Server.PrintToChatAll(ModuleName + " You can now steal only DEAD PLAYERS."); break;
        }
    }

    public void Pay(CCSPlayerController player, CCSPlayerController victim, int cashnum)
    {
        if (!PayToggle)
        {
            player.ExecuteClientCommand($"play sounds/ui/menu_invalid.vsnd");
            player.PrintToCenter("CashPay Plugin had been TurnOff");
            return;
        }

        if (victim.InGameMoneyServices is null) return;
        if (player.InGameMoneyServices is null) return;

        if (cashnum > 0 && cashnum >= player.InGameMoneyServices.Account) cashnum = player.InGameMoneyServices.Account;
        if (cashnum < 0 && cashnum * -1 >= victim.InGameMoneyServices.Account) cashnum = (victim.InGameMoneyServices.Account * -1);

        if (cashnum == 0)
        {
            if (victim.InGameMoneyServices.Account == 0)
            {
                victim.PrintToCenter("你被偷的底裤都不剩了!");
                player.PrintToCenter("他已经身无分文了! 鳖偷了!");
            }
            return;
        }

        player.InGameMoneyServices.Account -= cashnum;
        victim.InGameMoneyServices.Account += cashnum;
        player.ExecuteClientCommand($"play sounds/ui/armsrace_level_up_e.vsnd");
        victim.ExecuteClientCommand($"play sounds/ui/armsrace_level_up_e.vsnd");
        if (cashnum > 0)
        {
            player.PrintToCenter($"赠与 {victim.PlayerName} ${cashnum}");
            victim.PrintToCenter($"慷慨的 {player.PlayerName} 赠与了你 ${cashnum}");
        }
        else
        {
            victim.PrintToCenter($"你被沟槽的 {player.PlayerName} 偷了 ${cashnum * -1}");
            player.PrintToCenter($"你偷了 {victim.PlayerName} ${cashnum * -1}");
        }

        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pInGameMoneyServices");
    }

    private static double Distance3D(Vector3 vec1, Vector3 vec2)
    {
        var a = Math.Pow(vec2.X - vec1.X, 2);
        var b = Math.Pow(vec2.Y - vec1.Y, 2);
        var c = Math.Pow(vec2.Z - vec1.Z, 2);
        return Math.Sqrt(a + b + c);
    }
}
