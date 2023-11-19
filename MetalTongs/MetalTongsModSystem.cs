using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using metaltongs.network;
using metaltongs.entitybehavior;
using Vintagestory.API.Common.Entities;

namespace metaltongs
{
    class MetalTongsModSystem : ModSystem
    {
        private IServerNetworkChannel serverChannel;
        private ICoreAPI api;

        public override void StartPre(ICoreAPI api)
        {
            string cfgFileName = "MetalTongs.json";

            try 
            {
                MetalTongsConfig cfgFromDisk;
                if ((cfgFromDisk = api.LoadModConfig<MetalTongsConfig>(cfgFileName)) == null)
                {
                    api.StoreModConfig(MetalTongsConfig.Loaded, cfgFileName);
                }
                else
                {
                    MetalTongsConfig.Loaded = cfgFromDisk;
                }
            } 
            catch 
            {
                api.StoreModConfig(MetalTongsConfig.Loaded, cfgFileName);
            }

            base.StartPre(api);
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);

            api.RegisterEntityBehaviorClass("MetalTongs_EntityBehaviorDegradeTongsOnUse", typeof(EntityBehaviorDegradeTongsDuringUse));
            api.Event.OnEntitySpawn += AddEntityBehaviors;

            api.Logger.Notification("Loaded Metal Tongs!");
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            // Send connecting players config settings
            this.serverChannel.SendPacket(
                new SyncConfigClientPacket {
                    TongsUsageConsumesDurability = MetalTongsConfig.Loaded.TongsUsageConsumesDurability
                }, player);
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.Event.PlayerJoin += this.OnPlayerJoin; 
            
            // Create server channel for config data sync
            this.serverChannel = sapi.Network.RegisterChannel("metaltongs")
                .RegisterMessageType<SyncConfigClientPacket>()
                .SetMessageHandler<SyncConfigClientPacket>((player, packet) => {});
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            // Sync config settings with clients
            capi.Network.RegisterChannel("metaltongs")
                .RegisterMessageType<SyncConfigClientPacket>()
                .SetMessageHandler<SyncConfigClientPacket>(p => {
                    this.Mod.Logger.Event("Received config settings from server");
                    MetalTongsConfig.Loaded.TongsUsageConsumesDurability = p.TongsUsageConsumesDurability;
                });
        }
        
        public override void Dispose()
        {
            if (this.api is ICoreServerAPI sapi)
            {
                sapi.Event.PlayerJoin -= this.OnPlayerJoin;
            }
        }

        private void AddEntityBehaviors(Entity entity)
        {
            if (entity is EntityPlayer) entity.AddBehavior(new EntityBehaviorDegradeTongsDuringUse(entity));
        }
    }
}
