
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
public partial class Minigames : BasePlugin
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "Minigames misc plugin";
    public override string ModuleName => "Minigames";
    public override string ModuleVersion => "1.0.0";

    public string PL_PREFIX = $" [{ChatColors.Green}MiniGames{ChatColors.Default}] ";

    public Timer? g_TraiterTimer;
    public int g_iTraiterCountdown = 3;

    public bool g_bRespawn = false;
    public long[] g_iLastDeathTime = new long[64 + 1];

    public FakeConVar<bool> AutoRespawn = new("css_auto_respawn", "Auto detect repeat killer and disable respawn", false, ConVarFlags.FCVAR_RELEASE);
    public FakeConVar<bool> SetPushScale = new("css_set_pushscale", "Set phys_pushscale on round start", false, ConVarFlags.FCVAR_RELEASE);
    public FakeConVar<bool> ShuffleAtRoundEnd = new("css_shuffle_on_round_end", "Auto shuffle teams on round end", false, ConVarFlags.FCVAR_RELEASE);

    public bool IsPluginEnable()
    {
        return AutoRespawn.Value || SetPushScale.Value || ShuffleAtRoundEnd.Value;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        string functionNotice = "已开启功能: ";
        if (AutoRespawn.Value)
        {
            for (int i = 0; i <= 64; i++)
            {
                g_iLastDeathTime[i] = 0;
            }

            functionNotice += $" 重生 ";
            ToggleRespawn(true);
        }

        if (g_TraiterTimer != null)
        {
            g_TraiterTimer.Kill();
            g_TraiterTimer = null;
        }

        if (ShuffleAtRoundEnd.Value)
        {
            functionNotice += $" 25仔 ";
        }

        if (SetPushScale.Value)
        {
            var phys_pushscale = ConVar.Find("phys_pushscale");
            if (phys_pushscale != null)
            {
                phys_pushscale.Flags = ConVarFlags.FCVAR_RELEASE;
                phys_pushscale.SetValue(250.0f);
            }
            else
            {
                Server.PrintToChatAll($"{PL_PREFIX}PhysFix: Unable to find ConVar phys_pushscale!");
            }

            functionNotice += $" 物理增强 ";
        }

        if (IsPluginEnable())
        {
            Server.PrintToChatAll($"{PL_PREFIX}欢迎来到 ASOUL 小游戏服务器, 玩得开心!");
            Server.PrintToChatAll($"{PL_PREFIX}{functionNotice}");
        }

        return HookResult.Continue;
    }

    static void ListShuffle<T>(List<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo _)
    {
        if (!ShuffleAtRoundEnd.Value)
        {
            return HookResult.Continue;
        }

        g_iTraiterCountdown = 3;

        g_TraiterTimer = AddTimer(1.0f, () =>
        {
            if (g_iTraiterCountdown > 0)
            {
                VirtualFunctions.ClientPrintAll(HudDestination.Alert,
                $"25 仔将在 {g_iTraiterCountdown} 秒后出现",
                0, 0, 0, 0);
                g_iTraiterCountdown--;
                return;
            }

            var players = Utilities.GetPlayers().Where(players => players.Team >= CsTeam.Terrorist).ToList();
            ListShuffle(players);
            bool isTr = false;
            foreach (var p in players)
            {
                p.SwitchTeam(isTr ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
                isTr = !isTr;
            }

            VirtualFunctions.ClientPrintAll(HudDestination.Alert,
            $"25 仔已出现",
            0, 0, 0, 0);

            if (g_TraiterTimer != null)
            {
                g_TraiterTimer.Kill();
                g_TraiterTimer = null;
            }
        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
    {
        CCSPlayerController? player = @event.Userid;
        if (player != null)
        {
            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
            if (g_iLastDeathTime[player.Index] - unixTime <= 2)
            {
                Server.PrintToChatAll($"{PL_PREFIX}检测到复活点杀手, 重生已关闭. 死亡玩家将于下回合再次重生.");
                ToggleRespawn(false);
            }
        }

        return HookResult.Continue;
    }

    public void ToggleRespawn(bool enable = false)
    {
        ConVar.Find("mp_respawn_on_death_ct")!.SetValue(enable);
        ConVar.Find("mp_respawn_on_death_t")!.SetValue(enable);
    }
}
