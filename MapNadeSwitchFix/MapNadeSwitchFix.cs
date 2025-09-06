
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

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
            var grenade = (CBaseCSGrenade)entity;
            var designerName = entity.DesignerName;
            if (designerName == "weapon_hegrenade")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.HIGH_EXPLOSIVE_GRENADE;
            }
            else if (designerName == "weapon_smokegrenade")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.SMOKE_GRENADE;
            }
            else if (designerName == "weapon_flashbang")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.FLASHBANG;
            }
            else if (designerName == "weapon_molotov")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.MOLOTOV;
            }
            else if (designerName == "weapon_incgrenade")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.INCENDIARY_GRENADE;
            }
            else if (designerName == "weapon_decoy")
            {
                grenade.SubclassID.Value = (uint)ItemDefinition.DECOY_GRENADE;
            }
        });
    }
}
