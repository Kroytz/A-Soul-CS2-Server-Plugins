
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;


public partial class FuckValveMemoryLeak : BasePlugin
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "";
    public override string ModuleName => "Fuck Valve Memory Leak";
    public override string ModuleVersion => "1.0.0";

    private bool _fakeHibernate = false;
    private Timer? _timer;
    private Timer? _timerMapChange;
    private string _mapName = "";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            _fakeHibernate = false;
            _mapName = mapName;

            _timer = AddTimer(60.0f * 60.0f, () =>
            {
                var playing = Utilities.GetPlayers().Where(players => players.Connected == PlayerConnectedState.PlayerConnected && players.IsValid && !players.IsBot && !players.IsHLTV).Count();
                if (playing <= 0)
                {
                    _fakeHibernate = true;
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);

            _timerMapChange = AddTimer(120.0f * 60.0f, () =>
            {
                Server.ExecuteCommand($"map {_mapName}");
            }, TimerFlags.STOP_ON_MAPCHANGE);
        });

        RegisterListener<Listeners.OnClientConnected>((slot) =>
        {
            if (_fakeHibernate)
            {
                Server.ExecuteCommand($"map {_mapName}");
            }
        });
    }
}
