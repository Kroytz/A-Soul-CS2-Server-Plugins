
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

namespace Minigames;

[MinimumApiVersion(197)]
public partial class Cheer : BasePlugin
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "";
    public override string ModuleName => "Cheer";
    public override string ModuleVersion => "1.0.0";

    public FakeConVar<bool> EndRoundOnly = new("css_cheer_endround_only", "Only allow cheer on endround?", false, ConVarFlags.FCVAR_RELEASE);

    public Timer? g_CheerRegainTimer;
    public int[] g_iCheerRemain = new int[64 + 1];

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        bool endround = EndRoundOnly.Value;
        for (int i = 0; i <= 64; i++)
        {
            g_iCheerRemain[i] = endround ? -1 : 5;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo _)
    {
        if (EndRoundOnly.Value)
        {
            for (int i = 0; i <= 64; i++)
            {
                g_iCheerRemain[i] = 5;
            }
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("cheer", "Cheer")]
    [ConsoleCommand("css_cheer", "Cheer")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCheerCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player is null)
        {
            return;
        }

        if (g_iCheerRemain[player.Index] == -1)
        {
            player.PrintToCenter($"笑声仅在回合结束后可用!");
            return;
        }

        if (g_iCheerRemain[player.Index] <= 0)
        {
            player.PrintToCenter($"你已经笑不出声了!");
            return;
        }

        g_iCheerRemain[player.Index]--;
        var plList = Utilities.GetPlayers().Where(players => players.Connected == PlayerConnectedState.PlayerConnected && players.IsValid).ToList();
        if (player.PawnIsAlive)
        {
            Random random = new Random();
            var rnd = 1 + random.NextInt64() % 15;
            foreach (var p in plList)
            {
                p.ExecuteClientCommand($"play moeub/cheer/{rnd}.vsnd");
            }
            Server.PrintToChatAll($" {ChatColors.ForTeam(player.Team)}{player.PlayerName}{ChatColors.Default} cheered!!!");
        }
        else
        {
            foreach (var p in plList)
            {
                player.ExecuteClientCommand($"play asoul/jeer.vsnd");
            }
            Server.PrintToChatAll($" {ChatColors.ForTeam(player.Team)}{player.PlayerName}{ChatColors.Default} jeered!");
        }

        player.PrintToCenter($"Cheer 剩余 {g_iCheerRemain[player.Index]} 次");
    }
}
