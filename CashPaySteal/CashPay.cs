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

namespace cashpay;

public class CashPay : BasePlugin
{
    public override string ModuleName => "[CashPay]";
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
            CCSPlayerController? victim = null;

            var playerEntities = Utilities.GetPlayers().Where(players => players.Team >= CsTeam.Terrorist).ToList();

            foreach (var player in playerEntities)
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn is null) continue;
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

            var host = @event.Userid;

            if (host.PlayerPawn.Value is null) return HookResult.Continue;
            if (victim is null) return HookResult.Continue;
            if (victim.PlayerPawn.Value is null) return HookResult.Continue;

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

        foreach (CCSPlayerController target in players)
        {
            target.VoiceFlags = VoiceFlags.Muted;
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
    [CommandHelper(minArgs: 3, usage: "[userid1],[userid2],[cashnum]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPayForce(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var _userid = commandInfo.GetArg(1);
        var _userid_2 = commandInfo.GetArg(2);
        var _cashnum = commandInfo.GetArg(3);
        int userid = Int32.Parse(_userid);
        int userid_2 = Int32.Parse(_userid_2);
        int cashnum = Int32.Parse(_cashnum);

        player = Utilities.GetPlayerFromUserid(userid);
        var victim = Utilities.GetPlayerFromUserid(userid_2);

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
            player.PrintToCenter("Get More Money First");
            return;
        }

        if (cashnum >= player.InGameMoneyServices.Account) cashnum = player.InGameMoneyServices.Account;

        Pay(player, victim, cashnum);

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
    public void OnPaySteal(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var temp = commandInfo.GetArg(1);
        if (temp != "0" || temp != "1" || temp != "2") temp = "0";
        PaySteal = Int32.Parse(temp);
        switch (PaySteal)
        {
            case 0: Server.PrintToChatAll(ModuleName + "You Can Steal live or dead."); break;
            case 1: Server.PrintToChatAll(ModuleName + "You Can Steal only live."); break;
            case 2: Server.PrintToChatAll(ModuleName + "You Can Steal only dead."); break;
        }
        return;
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
            if (victim.InGameMoneyServices.Account == 0) victim.PrintToCenter("You have been bankruptcy !");
            if (player.InGameMoneyServices.Account == 0) player.PrintToCenter("You got no money !");
            return;
        }

        player.InGameMoneyServices.Account -= cashnum;
        victim.InGameMoneyServices.Account += cashnum;
        player.ExecuteClientCommand($"play sounds/ui/armsrace_level_up_e.vsnd");
        victim.ExecuteClientCommand($"play sounds/ui/armsrace_level_up_e.vsnd");
        if (cashnum > 0)
        {
            player.PrintToCenter("Pay " + victim.PlayerName.ToString() + " $" + cashnum.ToString());
            victim.PrintToCenter("Receive " + " $" + cashnum.ToString() + "from" + player.PlayerName.ToString());
        }
        else
        {
            victim.PrintToCenter(" $" + (cashnum * -1).ToString() + " stolen by " + player.PlayerName.ToString());
            player.PrintToCenter("Steal " + victim.PlayerName.ToString() + " $" + (cashnum * -1).ToString());
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
