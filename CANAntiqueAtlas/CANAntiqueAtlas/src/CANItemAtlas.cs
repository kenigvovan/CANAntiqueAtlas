using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.gui;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;
using static CANAntiqueAtlas.src.core.IBiomeDetector;
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
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                return;
            }          
            AtlasData data = CANAntiqueAtlas.atlasData.GetAtlasData(slot.StackSize, byEntity.World);
            if (data == null || !(byEntity is EntityPlayer)) return;

            // On the first run send the map from the server to the client:
            EntityPlayer player = (EntityPlayer)byEntity;
            if (!(byEntity.World.Side == EnumAppSide.Client) && data.isSyncedOnPlayer(player) && !data.isEmpty())
            {
                data.syncOnPlayer(slot.Itemstack.Attributes.GetInt("atlasID"), player);
            }

            // Same thing with the local markers:
            /*MarkersData markers = AntiqueAtlasMod.markersData.getMarkersData(stack, world);
            if (!world.isRemote && !markers.isSyncedOnPlayer(player) && !markers.isEmpty())
            {
                markers.syncOnPlayer(stack.getItemDamage(), player);
            }
            markers = null;*/

            // Update the actual map only so often:
            int newScanInterval = (int)Math.Round(CANAntiqueAtlas.config.newScanInterval * 20);
            int rescanInterval = newScanInterval * CANAntiqueAtlas.config.rescanRate;
            if (new Random().Next(0, 1000) > 5)
            {
                return;
            }

            int playerX = (int)(Math.Floor(player.Pos.X)) >> 5;
            int playerZ = (int)(Math.Floor(player.Pos.Z)) >> 5;
            ITileStorage seenChunks = data.GetDimensionData();
            IBiomeDetector biomeDetector = CANAntiqueAtlas.biomeDetector;
            int scanRadius = CANAntiqueAtlas.config.scanRadius;

            // Look at chunks around in a circular area:
            for (double dx = -scanRadius; dx <= scanRadius; dx++)
            {
                for (double dz = -scanRadius; dz <= scanRadius; dz++)
                {
                    /*if (dx * dx + dz * dz > scanRadius * scanRadius)
                    {
                        continue; // Outside the circle
                    }*/
                    int x = (int)(playerX + dx);
                    int z = (int)(playerZ + dz);
                    Tile oldTile = seenChunks.GetTile(x, z);

                    // Check if there's a custom tile at the location:
                    //int biomeId = CANAntiqueAtlas.extBiomeData.getData().getBiomeIdAt(0, x, z);
                    // Custom tiles overwrite even the chunks already seen.
                    BiomeType biomeId = BiomeType.NOT_FOUND;
                    // If there's no custom tile, check the actual chunk:
                    if (/*biomeId == -1*/true)
                    {
                        var chunkCoords = new Vec2i(x, z);
                        var chunk = byEntity.World.BlockAccessor.GetMapChunk(chunkCoords);
                        //Chunk chunk = player.worldObj.getChunkFromChunkCoords(x, z);
                        // Force loading of chunk, if required:
                        if (chunk == null)
                        {
                            if (CANAntiqueAtlas.config.forceChunkLoading)
                            {
                                CANAntiqueAtlas.sapi.WorldManager.LoadChunkColumn(x, z);
                                //player.worldObj.getChunkProvider().loadChunk(x << 4, z << 4);
                                continue;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        // Skip chunk if it hasn't loaded yet:
                        /*if (!chunk.isChunkLoaded)
                        {
                            continue;
                        }*/

                        if (oldTile != null)
                        {
                            // If the chunk has been scanned previously, only re-scan it so often:
                            if (!CANAntiqueAtlas.config.doRescan || (player.World.ElapsedMilliseconds / 1000) % rescanInterval != 0)
                            {
                                continue;
                            }
                            
                            biomeId = biomeDetector.GetBiomeID(chunk, chunkCoords);
                            if (biomeId == BiomeType.NOT_FOUND)
                            {
                                // If the new tile is empty, remove the old one:
                                data.RemoveTile(0, x, z);
                            }
                            else if (oldTile.biomeID != (int)biomeId)
                            {
                                // Only update if the old tile's biome ID doesn't match the new one:
                                data.SetTile(0, x, z, new Tile((int)biomeId));
                            }
                        }
                        else
                        {
                            // Scanning new chunk:
                            biomeId = biomeDetector.GetBiomeID(chunk, chunkCoords);
                            if (biomeId != BiomeType.NOT_FOUND)
                            {
                                data.SetTile(0, x, z, new Tile((int)biomeId));
                            }
                        }
                    }
                    else
                    {
                        // Only update the custom tile if it doesn't rewrite itself:
                        /*if (oldTile == null || oldTile.biomeID != biomeId)
                        {
                            data.SetTile(0, x, z, new Tile(biomeId));
                            data.Save();
                        }*/
                    }

                }
            }
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
                var modsys = byEntity.Api.ModLoader.GetModSystem<CANWorldMapManager>();
                modsys.OnHotKeyWorldMapDlg(null, atlasId);
                handling = EnumHandHandling.PreventDefault;
            }
        }
    }
}
