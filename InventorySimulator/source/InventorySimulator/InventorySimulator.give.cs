﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using System.Reflection;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void GivePlayerMusicKit(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("mk")) return;
        player.InventoryServices.MusicID = inventory.GetUShort("mk");
    }

    public void GivePlayerPin(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("pi")) return;
        for (var index = 0; index < player.InventoryServices.Rank.Length; index++)
        {
            player.InventoryServices.Rank[index] = index == 5 ? (MedalRank_t)inventory.GetUInt("pi") : MedalRank_t.MEDAL_RANK_NONE;
        }
    }

    public void GivePlayerGloves(CCSPlayerController player)
    {
        try
        {
            if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.EconGloves == null) return;
            var inventory = GetPlayerInventory(player);

            var team = player.TeamNum;
            if (!inventory.HasProperty("gl", team)) return;

            var itemDef = inventory.GetUShort("gl", team);
            if (!inventory.HasProperty("pa", team, itemDef)) return;

            foreach (var wearable in player.PlayerPawn.Value.MyWearables)
            {
                if (wearable.Value != null && wearable.IsValid)
                {
                    wearable.Value.AcceptInput("KillHierarchy");
                }
            }

            string model = player.PlayerPawn.Value.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? string.Empty;
            if (!string.IsNullOrEmpty(model))
            {
                player.PlayerPawn.Value.SetModel("characters/models/tm_jumpsuit/tm_jumpsuit_varianta.vmdl");
                player.PlayerPawn.Value.SetModel(model);
            }

            player.PlayerPawn.Value.MyWearables.RemoveAll();
            var glove = player.PlayerPawn.Value.EconGloves;
            glove.ItemDefinitionIndex = itemDef;
            UpdatePlayerEconItemID(glove);

            Server.NextFrame(() =>
            {
                glove.Initialized = true;
                glove.NetworkedDynamicAttributes.Attributes.RemoveAll();
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture prefab", inventory.GetInt("pa", team, itemDef, 0));
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture seed", inventory.GetInt("se", team, itemDef, 1));
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture wear", inventory.GetFloat("fl", team, itemDef, 0.0f));
                SetBodygroup(player, "default_gloves");
            });
        } catch (Exception)
        {
            // Getting `m_EconGloves` may throw an exception as it is marked as non-nullable in the schema, but it can still be NULL.
            // The check we do in the beggining of the function has no effect it seems.
            // This should take care of TargetInvocationException exceptions.
        }
    }

    public void GivePlayerAgent(CCSPlayerController player)
    {
        if (MinModelsCvar.Value > 0)
        {
            // For now any value non-zero will force SAS & Phoenix.
            // In the future: 1 - Map agents only, 2 - SAS & Phoenix.
            if (player.Team == CsTeam.Terrorist)
                SetPlayerModel(player, "characters/models/tm_phoenix/tm_phoenix.vmdl");

            if (player.Team == CsTeam.CounterTerrorist)
                SetPlayerModel(player, "characters/models/ctm_sas/ctm_sas.vmdl");

            return;
        }

        var team = player.TeamNum;
        var inventory = GetPlayerInventory(player);
        var model = inventory.GetString("agm", team);

        if (model == null) return;

        SetPlayerModel(player, GetAgentModelPath(model));
    }

    public void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        var isKnife = IsKnifeClassName(weapon.DesignerName);
        var inventory = GetPlayerInventory(player);
        var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        var team = player.TeamNum;
        var hasKnife = inventory.HasProperty("me", team);
        var isCustom = inventory.HasProperty("cw", team, itemDef);

        if (!hasKnife && !isCustom) return;

        if (isKnife)
        {
            itemDef = inventory.GetUShort("me", team);
            if (weapon.AttributeManager.Item.ItemDefinitionIndex != itemDef)
            {
                SubclassChange(weapon, itemDef);
            }
            weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;
            weapon.AttributeManager.Item.EntityQuality = 3;
        }

        var paintKit = inventory.GetInt("pa", team, itemDef, 0);
        if (paintKit == 0) return;

        UpdatePlayerEconItemID(weapon.AttributeManager.Item);

        var seed = inventory.GetInt("se", team, itemDef, 1);
        var wear = inventory.GetFloat("fl", team, itemDef, 0.0f);
        weapon.FallbackPaintKit = paintKit;
        weapon.FallbackSeed = seed;
        weapon.FallbackWear = wear;
        weapon.FallbackStatTrak = inventory.GetInt("st", team, itemDef, -1);
        weapon.AttributeManager.Item.CustomName = inventory.GetString("nt", team, itemDef, "");
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();

        // This APPEARS to fix the issue where sometimes the skin name won't be displayed on HUD.
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture prefab", paintKit);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture seed", seed);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture wear", wear);

        if (!isKnife)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                // For setting the id of the sticker, we need to do a bit of trickery. In items_game.txt if you look for `sticker slot 0 id`, you will
                // see that `stored_as_integer` is marked with `1`, so we basically need to view a `uint` as a `float`, e.g. the value stored in the
                // address of the `uint` will be interpreted as it was a `float` type (e.g.: uint stickerId = 2229 -> float stickerId = 3.12349e-42f)
                // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blame/main/game/shared/econ/econ_item_view.cpp#L194
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, $"sticker slot {slot} id", inventory.GetIntAsFloat("ss", team, itemDef, slot, 0));
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, $"sticker slot {slot} wear", inventory.GetFloat("sf", team, itemDef, slot, 0.0f));
            }
            UpdatePlayerWeaponMeshGroupMask(player, weapon, inventory.HasProperty("pal", team, itemDef));
        }
    }
}
