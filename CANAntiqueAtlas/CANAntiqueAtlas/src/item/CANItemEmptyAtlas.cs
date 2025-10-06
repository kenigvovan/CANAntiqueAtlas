using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.item
{
    public class CANItemEmptyAtlas: Item
    {
        //public override onheld
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
            handling = EnumHandHandling.PreventDefault;
            if (byEntity.World.Side == EnumAppSide.Client || slot.Empty) 
            {
                return;
            }
            long atlasID = CANItemAtlas.GetNextAtlasID();
            ItemStack atlasStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("canantiqueatlas:canatlas")), 1);
            atlasStack.Attributes.SetLong("atlasID", atlasID);

            AtlasData atlasData = CANAntiqueAtlas.atlasData.GetAtlasData(atlasID, byEntity.World);
            atlasData.GetDimensionData().setBrowsingPosition((int)Math.Round(-byEntity.ServerPos.X * CANAntiqueAtlas.config.defaultScale),
                    (int)Math.Round(-byEntity.ServerPos.Z * CANAntiqueAtlas.config.defaultScale),
                     CANAntiqueAtlas.config.defaultScale);
            slot.Itemstack.StackSize--;
            slot.MarkDirty();
            /*MarkersData markersData = AntiqueAtlasMod.markersData.getMarkersData(atlasID, world);
            markersData.markDirty();*/
            (byEntity as EntityPlayer).Player?.InventoryManager.TryGiveItemstack(atlasStack);
            
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
    }
}
