using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace metaltongs.entitybehavior
{
    /// <summary>
    /// Credits to DanaCraluminum and NoHeatResistantInventory for use of entity behavior to handle hot items;
    /// we will do similar here to handle tong damage on use
    /// </summary>
    public class EntityBehaviorDegradeTongsDuringUse : EntityBehavior
    {
	
        public EntityBehaviorDegradeTongsDuringUse(Entity entity) : base(entity) {}

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

			this.tickAccum = (float)(this.entity.World.Rand.NextDouble() * 0.50);
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

			// Exit early if we arn't damaging tongs
			if (!MetalTongsConfig.Loaded.TongsUsageConsumesDurability)
				return;

			if ((this.tickAccum += deltaTime) < 0.50f)
			{
				return;
			}
			this.tickAccum = 0f;

            if (entity is EntityPlayer entityPlayer)
            {
				IPlayer player = entityPlayer.Player;

				// Uhh
				if (player.InventoryManager.GetHotbarInventory() is InventoryBase invBase)
				{
					ItemSlot slotOfItemHeldWithTongs = entityPlayer.RightHandItemSlot;
					TryDamageTongsInUse(invBase, slotOfItemHeldWithTongs, player);
				}
            }
        }

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
				isHeatResistant = (attributes != null) ? new bool?(attributes.IsTrue("heatResistant")) : null;
			}
			return isHeatResistant.GetValueOrDefault() ? itemstack : null;
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


			// Try to find tongs and damage them if we are configured to do so
			ItemSlot tongsSlot = player.Entity.LeftHandItemSlot; // FIXME: This should be passed by ref from GetHeatREsistantHandGear 
			ItemStack tongsItemStack = GetHeatResistantHandGear(player); 
			
			if (tongsItemStack == null)
			{
				return;
			}

			// We need to damage only when working with hot items
			if (IsWorkingWithHotItem(inventory, slot))
			{
				tongsItemStack.Collectible.DamageItem(player.Entity.World, player.Entity, tongsSlot, 1);  

				// If tongs broke, we need to force drop the item
				if (tongsItemStack.Collectible.GetRemainingDurability(tongsItemStack) <= 0)
				{
					inventory.DropSlotIfHot(slot, player) ;
				}
			}
			 	
        }

		public override string PropertyName() => "degradetongsduringuse";

		private float tickAccum;
    }
}