
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

public partial class MapNadeSwitchFix : BasePlugin
{
    public override string ModuleAuthor => "Kroytz";
    public override string ModuleDescription => "";
    public override string ModuleName => "Map Nade Switch Fix";
    public override string ModuleVersion => "1.0.0";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnEntityCreated>((entity) =>
        {
            // Shit CS# WHY I HAVE TO CAST CLASS LIKE THIS
            var hGrenade = (CHandle<CBaseCSGrenade>)Activator.CreateInstance(typeof(CHandle<CBaseCSGrenade>), entity.EntityHandle.Raw)!;
            if (!hGrenade.IsValid)
                return;

            var grenade = hGrenade.Get();
            if (grenade == null || !grenade.IsValid)
                return;

            var designerName = entity.DesignerName;
            if (designerName == "weapon_hegrenade")
            {
                Server.NextFrame(() =>
                {
                    var retard = (ushort)ItemDefinition.HIGH_EXPLOSIVE_GRENADE;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard.ToString());
                });
            }
            else if (designerName == "weapon_smokegrenade")
            {
                Server.NextFrame(() =>
                {
                    var retard2 = (ushort)ItemDefinition.SMOKE_GRENADE;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard2.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard2.ToString());
                });
            }
            else if (designerName == "weapon_flashbang")
            {
                Server.NextFrame(() =>
                {
                    var retard3 = (ushort)ItemDefinition.FLASHBANG;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard3.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard3.ToString());
                });
            }
            else if (designerName == "weapon_molotov")
            {
                Server.NextFrame(() =>
                {
                    var retard4 = (ushort)ItemDefinition.MOLOTOV;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard4.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard4.ToString());
                });
            }
            else if (designerName == "weapon_incgrenade")
            {
                Server.NextFrame(() =>
                {
                    var retard5 = (ushort)ItemDefinition.INCENDIARY_GRENADE;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard5.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard5.ToString());
                });
            }
            else if (designerName == "weapon_decoy")
            {
                Server.NextFrame(() =>
                {
                    var retard6 = (ushort)ItemDefinition.DECOY_GRENADE;
                    Server.PrintToConsole($"[NadeSwitchFix] Fixing {designerName} subclass {retard6.ToString()} for {grenade.Index}");
                    grenade.AcceptInput("ChangeSubclass", null, null, retard6.ToString());
                });
            }
        });
    }
}
