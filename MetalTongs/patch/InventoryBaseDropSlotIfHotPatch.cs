using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace metaltongs.patch
{
	[HarmonyPatch(typeof(InventoryBase), "DropSlotIfHot")]
	class InventoryBaseDropSlotIfHotPatch
	{
        private static ItemStack GetHeatResistantHandGear(IPlayer player)
		{
			if (player == null)
			{
				return null;
			}
			ItemSlot leftHandItemSlot = player.Entity.LeftHandItemSlot;
			if (leftHandItemSlot == null)
			{
				return null;
			}
			ItemStack itemstack = leftHandItemSlot.Itemstack;
            bool? isHeatResistant = false;
			if (itemstack == null)
			{
				return null;
			}
			else
			{
				JsonObject attributes = itemstack.Collectible.Attributes;
				isHeatResistant = ((attributes != null) ? new bool?(attributes.IsTrue("heatResistant")) : null);
			}
			return (isHeatResistant.GetValueOrDefault()) ? itemstack : null;
		}

		private static bool IsWorkingWithHotItem(InventoryBase inventory, ItemSlot hotItemSlot)
		{
			JsonObject attributes = hotItemSlot.Itemstack.Collectible.Attributes;

			return (attributes == null || !attributes.IsTrue("allowHotCrafting")) && hotItemSlot.Itemstack.Collectible.GetTemperature(inventory.Api.World, hotItemSlot.Itemstack) > 300f;
		}

        private static void TryDamageTongsInUse(InventoryBase inventory, ItemSlot slot, IPlayer player = null)
        {
            if (inventory.Api.Side == EnumAppSide.Client)
			{
				return;
			}
			if (slot.Empty)
			{
				return;
			}
			if (player != null && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				return;
			}

            if (MetalTongsConfig.Loaded.TongsUsageConsumesDurability)
            {
                // Try to find tongs and damage them if we are configured to do so
                ItemStack tongsItemStack = GetHeatResistantHandGear(player); 
                
                if (tongsItemStack == null)
                {
                    return;
                }

				// We need to damage only when working with hot items
				if (IsWorkingWithHotItem(inventory, slot))
				{
					ItemSlot leftHandItemSlot = player.Entity.LeftHandItemSlot; // FIXME: This should be passed by ref from GetHeatREsistantHandGear
					tongsItemStack.Collectible.DamageItem(player.Entity.World, player.Entity, leftHandItemSlot, 1);   
				}
			 	
            }
        }

		[HarmonyPostfix]
		static void Postfix(InventoryBase __instance, ItemSlot slot, IPlayer player)
		{
			TryDamageTongsInUse(__instance, slot, player);
		}
	}
}

    