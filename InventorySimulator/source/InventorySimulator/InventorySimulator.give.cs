﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly FakeConVar<int> invsim_minmodels = new("invsim_minmodels", "Allows agents or use specific models for each team.", 0, flags: ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 2));

    public void GivePlayerMusicKit(CCSPlayerController player)
    {
        if (!IsPlayerHumanAndValid(player)) return;
        if (player.InventoryServices == null) return;
        if (MusicKitManager.TryGetValue(player.SteamID, out var musicKit))
        {
            player.InventoryServices.MusicID = (ushort)musicKit.Def;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
            player.MusicKitID = musicKit.Def;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");
            player.MusicKitMVPs = musicKit.Stattrak;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitMVPs");
        }
    }

    public void GivePlayerPin(CCSPlayerController player, PlayerInventory inventory)
    {
        if (player.InventoryServices == null) return;

        var pin = inventory.Pin;
        if (pin == null) return;

        for (var index = 0; index < player.InventoryServices.Rank.Length; index++)
        {
            player.InventoryServices.Rank[index] = index == 5 ? (MedalRank_t)pin.Value : MedalRank_t.MEDAL_RANK_NONE;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }
    }

    public void GivePlayerGloves(CCSPlayerController player, PlayerInventory inventory)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.Handle == IntPtr.Zero)
            return;

        if (inventory.Gloves.TryGetValue(player.TeamNum, out var item))
        {
            var glove = pawn.EconGloves;
            Server.NextFrame(() =>
            {
                glove.Initialized = true;
                glove.ItemDefinitionIndex = item.Def;
                UpdatePlayerEconItemID(glove);

                glove.NetworkedDynamicAttributes.Attributes.RemoveAll();
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes.Handle, "set item texture prefab", item.Paint);
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes.Handle, "set item texture seed", item.Seed);
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes.Handle, "set item texture wear", item.Wear);

                glove.AttributeList.Attributes.RemoveAll();
                SetOrAddAttributeValueByName(glove.AttributeList.Handle, "set item texture prefab", item.Paint);
                SetOrAddAttributeValueByName(glove.AttributeList.Handle, "set item texture seed", item.Seed);
                SetOrAddAttributeValueByName(glove.AttributeList.Handle, "set item texture wear", item.Wear);

                SetBodygroup(pawn.Handle, "default_gloves", 1);
            });
        }
    }

    public void GivePlayerAgent(CCSPlayerController player, PlayerInventory inventory)
    {
        if (invsim_minmodels.Value > 0)
        {
            // For now any value non-zero will force SAS & Phoenix.
            // In the future: 1 - Map agents only, 2 - SAS & Phoenix.
            if (player.Team == CsTeam.Terrorist)
                SetPlayerModel(player, "characters/models/tm_phoenix/tm_phoenix.vmdl");

            if (player.Team == CsTeam.CounterTerrorist)
                SetPlayerModel(player, "characters/models/ctm_sas/ctm_sas.vmdl");

            return;
        }

        if (inventory.Agents.TryGetValue(player.TeamNum, out var item))
        {
            var patches = item.Patches.Count != 5 ? Enumerable.Repeat((uint)0, 5).ToList() : item.Patches;
            SetPlayerModel(player, GetAgentModelPath(item.Model), item.VoFallback, item.VoPrefix, item.VoFemale, patches);
        }
    }

    public void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        if (IsCustomWeaponItemID(weapon)) return;

        var isKnife = IsKnifeClassName(weapon.DesignerName);
        var entityDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        var inventory = GetPlayerInventory(player);
        var item = isKnife ? inventory.GetKnife(player.TeamNum) : inventory.GetWeapon(player.Team, entityDef);
        if (item == null) return;

        if (isKnife)
        {
            if (entityDef != item.Def)
            {
                ChangeSubclass(weapon.Handle, item.Def.ToString());
            }
            weapon.AttributeManager.Item.ItemDefinitionIndex = item.Def;
            weapon.AttributeManager.Item.EntityQuality = 3;
        }
        else
        {
            weapon.AttributeManager.Item.EntityQuality = item.Stattrak >= 0 ? 9 : 0;
        }

        UpdatePlayerEconItemID(weapon.AttributeManager.Item);

        weapon.FallbackPaintKit = item.Paint;
        weapon.FallbackSeed = item.Seed;
        weapon.FallbackWear = item.Wear;
        weapon.AttributeManager.Item.CustomName = item.Nametag;
        weapon.AttributeManager.Item.AccountID = (uint)player.SteamID;

        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", item.Paint);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture seed", item.Seed);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture wear", item.Wear);

        weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture prefab", item.Paint);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture seed", item.Seed);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture wear", item.Wear);

        if (item.Stattrak >= 0)
        {
            weapon.FallbackStatTrak = item.Stattrak;
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "kill eater", ViewAsFloat(item.Stattrak));
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "kill eater score type", 0);
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "kill eater", ViewAsFloat(item.Stattrak));
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "kill eater score type", 0);
        }

        if (!isKnife)
        {
            foreach (var sticker in item.Stickers)
            {
                // To set the ID of the sticker, we need to use a workaround. In the items_game.txt file, locate the
                // sticker slot 0 id entry. It should be marked with stored_as_integer set to 1. This means we need to
                // treat a uint as a float. For example, if the uint stickerId is 2229, we would interpret its value as
                // if it were a float (e.g., float stickerId = 3.12349e-42f).
                // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blame/main/game/shared/econ/econ_item_view.cpp#L194
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, $"sticker slot {sticker.Slot} id", ViewAsFloat(sticker.Def));
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, $"sticker slot {sticker.Slot} wear", sticker.Wear);
            }
            UpdatePlayerWeaponMeshGroupMask(player, weapon, item.Legacy);
        }
    }

    public void GivePlayerWeaponStatTrakIncrease(
        CCSPlayerController player,
        string designerName,
        string weaponItemId)
    {
        try
        {
            var weaponServices = player.PlayerPawn.Value!.WeaponServices;
            if (weaponServices == null || weaponServices.ActiveWeapon == null)
                return;

            if (weaponServices.ActiveWeapon.Value == null || !weaponServices.ActiveWeapon.IsValid)
                return;

            var weapon = weaponServices.ActiveWeapon.Value;
            if (!IsCustomWeaponItemID(weapon) || weapon.FallbackStatTrak < 0)
                return;

            if (weapon.AttributeManager.Item.AccountID != (uint)player.SteamID)
                return;

            if (weapon.AttributeManager.Item.ItemID != ulong.Parse(weaponItemId))
                return;

            if (weapon.FallbackStatTrak >= 999_999)
                return;

            var isKnife = IsKnifeClassName(designerName);
            var newValue = weapon.FallbackStatTrak + 1;
            var def = weapon.AttributeManager.Item.ItemDefinitionIndex;
            weapon.FallbackStatTrak = newValue;
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "kill eater", ViewAsFloat(newValue));
            SetOrAddAttributeValueByName(weapon.AttributeManager.Item.AttributeList.Handle, "kill eater", ViewAsFloat(newValue));
            var inventory = GetPlayerInventory(player);
            var item = isKnife ? inventory.GetKnife(player.TeamNum) : inventory.GetWeapon(player.Team, def);
            if (item != null)
            {
                item.Stattrak = newValue;
                SendStatTrakIncrease(player.SteamID, item.Uid);
            }
        }
        catch
        {
            // Ignore any errors.
        }
    }

    public void GivePlayerMusicKitStatTrakIncrease(CCSPlayerController player)
    {
        if (MusicKitManager.TryGetValue(player.SteamID, out var musicKit))
        {
            musicKit.Stattrak += 1;
            SendStatTrakIncrease(player.SteamID, musicKit.Uid);
        }
    }

    public void GiveOnPlayerSpawn(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
        GivePlayerAgent(player, inventory);
        GivePlayerGloves(player, inventory);
    }

    public void GiveOnPlayerInventoryRefresh(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
    }

    // Nuke this when roflmuffin/CounterStrikeSharp#377 is resolved. This workaround makes sure the
    // subclass of a knife will be changed as soon as the player receives it. Only needed on Windows
    // because on Linux we hook GiveNamedItem that happens a bit early than this event.
    public void GiveOnItemPickup(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            var myWeapons = pawn.WeaponServices?.MyWeapons;
            if (myWeapons != null)
            {
                foreach (var handle in myWeapons)
                {
                    var weapon = handle.Value;
                    if (weapon != null && IsKnifeClassName(weapon.DesignerName))
                    {
                        GivePlayerWeaponSkin(player, weapon);
                    }
                }
            }
        }
    }
}
