using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src
{
    public class CANItemEmptyAtlas: Item
    {
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
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
            /*MarkersData markersData = AntiqueAtlasMod.markersData.getMarkersData(atlasID, world);
            markersData.markDirty();*/
            (byEntity as EntityPlayer).Player?.InventoryManager.TryGiveItemstack(atlasStack);
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
        }
    }
}
