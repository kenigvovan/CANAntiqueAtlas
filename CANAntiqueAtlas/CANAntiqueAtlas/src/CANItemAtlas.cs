using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.core.BiomeDetectors;
using CANAntiqueAtlas.src.gui;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;
using static Vintagestory.Server.Timer;

namespace CANAntiqueAtlas.src
{
    public class CANItemAtlas: Item
    {
        protected static long WORLD_ATLAS_DATA_ID = 0;
        public static object id_lock = new object();

        public static long GetNextAtlasID()
        {
            lock(id_lock)
            {
                return WORLD_ATLAS_DATA_ID++;
            }           
        }
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            base.OnHeldIdle(slot, byEntity);
            return;
        }
        public override void OnCollected(ItemStack stack, Entity entity)
        {
        }
        public override void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
        {
        }
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                long atlasId = slot?.Itemstack.Attributes.GetLong("atlasID", -1) ?? -1;
                if(atlasId < 0)
                {
                    //return;

                }
                atlasId = 0;
                var modsys = byEntity.Api.ModLoader.GetModSystem<CANWorldMapManager>();
                modsys.OnHotKeyWorldMapDlg(null, atlasId);
                handling = EnumHandHandling.PreventDefault;
            }
        }
    }
}
