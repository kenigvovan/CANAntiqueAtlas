using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.core.BiomeDetectors;
using CANAntiqueAtlas.src.gui;
using CANAntiqueAtlas.src.network.client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;
using Vintagestory.GameContent;
using static CANAntiqueAtlas.src.core.BiomeDetectors.IBiomeDetector;
using static Vintagestory.Server.Timer;

namespace CANAntiqueAtlas.src.item
{
    public class CANItemAtlas: Item
    {
        public static long WORLD_ATLAS_DATA_ID = 0;
        public static object id_lock = new object();

        public static long GetNextAtlasID()
        {
            lock(id_lock)
            {
                WORLD_ATLAS_DATA_ID++;
                CANAntiqueAtlas.sapi.WorldManager.SaveGame.StoreData<long>(CANAntiqueAtlas.WorldAtlasDataIdString, WORLD_ATLAS_DATA_ID);
                return WORLD_ATLAS_DATA_ID;
            }         
        }
        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            base.OnHeldIdle(slot, byEntity);
            return;
        }
        public override void OnCollected(ItemStack stack, Entity entity)
        {
            base.OnCollected(stack, entity);
            if (entity != null && entity.World.Side == EnumAppSide.Server)
            {
                long atlasId = stack?.Attributes.GetLong("atlasID", -1) ?? -1;
                if (entity != null && entity.Api.Side == EnumAppSide.Server && entity is EntityPlayer)
                {
                    CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.AddAtlasToPlayer((entity as EntityPlayer).Player as IServerPlayer, atlasId);
                }
            }
        }
        public override void OnHeldDropped(IWorldAccessor world, IPlayer byPlayer, ItemSlot slot, int quantity, ref EnumHandling handling)
        {
            base.OnHeldDropped(world, byPlayer, slot, quantity, ref handling);
            long atlasId = slot?.Itemstack.Attributes.GetLong("atlasID", -1) ?? -1;
            CANAntiqueAtlas.clientChannel.SendPacket(new DropedAtlasIdPacket { atlasID = atlasId });
        }
        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
        {
            base.OnModifiedInInventorySlot(world, slot, extractedStack);
            if (world.Side == EnumAppSide.Server) 
            {
                var inv = slot.Inventory;
                if (inv != null && (inv.ClassName.Equals("hotbar") || inv.ClassName.Equals("backpack")))
                {
                    long atlasId = slot?.Itemstack.Attributes.GetLong("atlasID", -1) ?? -1;
                    CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.AddAtlasToPlayer((inv as InventoryBasePlayer).Player as IServerPlayer, atlasId);
                //if(inv.)
                }
                else
                {
                    long atlasId = slot?.Itemstack.Attributes.GetLong("atlasID", -1) ?? -1;
                    CANAntiqueAtlas.sapi.ModLoader.GetModSystem<CANAntiqueAtlas>().pmls.ForAtlasIdRecheckPlayers(atlasId);
                }
            }
            //Console.WriteLine(inv.ClassName);
            //on q it is not called, need onDropped
            //if inventory is not players then check all players active with such id atlas and recalculate list
            //if it players then just add if not tracked already
        }
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                long atlasId = slot.Itemstack?.Attributes.GetLong("atlasID", -1) ?? -1;
                if(atlasId < 0)
                {
                    return;

                }
                var modsys = byEntity.Api.ModLoader.GetModSystem<CANWorldMapManager>();
                modsys.OnHotKeyWorldMapDlg(null, atlasId);
                handling = EnumHandHandling.PreventDefault;
            }
        }
        public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
        {
            base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
            long atlasId = -1;
            foreach (var it in allInputSlots)
            {
                if (it.Itemstack?.Item is CANItemAtlas && it.Itemstack.Attributes.HasAttribute("atlasID"))
                {
                    atlasId = it.Itemstack.Attributes.GetLong("atlasID", -1);
                }
            }
            if (atlasId >= 0)
            {
                stackInSlot.Itemstack.Attributes.SetLong("atlasID", atlasId);
            }
        }
        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
            long atlasId = -1;
            foreach (var it in allInputslots)
            {
                if (it.Itemstack?.Item is CANItemAtlas && it.Itemstack.Attributes.HasAttribute("atlasID"))
                {
                    atlasId = it.Itemstack.Attributes.GetLong("atlasID", -1);
                }
            }
            if (atlasId >= 0)
            {
                outputSlot.Itemstack.Attributes.SetLong("atlasID", atlasId);
            }
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if (!inSlot.Empty && inSlot.Itemstack.Attributes.HasAttribute("atlasID"))
            {
               var atlasId = inSlot.Itemstack.Attributes.GetLong("atlasID");
               dsc.Append(Lang.Get("canantiqueatlas:atlas-number-info", atlasId));
            }
        }
    }
}
