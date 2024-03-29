﻿
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

namespace Minigames;

[MinimumApiVersion(197)]
public partial class Minigames : BasePlugin
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "Minigames misc plugin";
    public override string ModuleName => "Minigames";
    public override string ModuleVersion => "1.0.0";

    public int g_iTraiterCountdown = 3;
    public FakeConVar<bool> SetPushScale = new("css_set_pushscale", "Set phys_pushscale on round start", false, ConVarFlags.FCVAR_RELEASE);
    public FakeConVar<bool> ShuffleAtRoundEnd = new("css_shuffle_on_round_end", "Auto shuffle teams on round end", false, ConVarFlags.FCVAR_RELEASE);

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        if (!SetPushScale.Value)
        {
            return HookResult.Continue;
        }

        var phys_pushscale = ConVar.Find("phys_pushscale");
        if (phys_pushscale != null)
        {
            phys_pushscale.Flags = ConVarFlags.FCVAR_RELEASE;
            phys_pushscale.SetValue(250.0f);
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

        AddTimer(1.0f, () => 
        { 
            if (g_iTraiterCountdown > 0)
            {
                VirtualFunctions.ClientPrintAll(HudDestination.Alert,
               $"25 仔将在 {g_iTraiterCountdown} 秒后出现",
                0, 0, 0, 0);
                g_iTraiterCountdown--;
            }
            else
            {
                var players = Utilities.GetPlayers().Where(players => players.Team >= CsTeam.Terrorist).ToList();
                ListShuffle(players);
                bool isTr = false;
                foreach (var p in players)
                {
                    p.SwitchTeam(isTr ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
                    isTr = !isTr;
                }
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }
}
