using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui.Map.TileLayer
{
    public class CANReadyMapPiece
    {
        public int[] Biome;
        public short[] VariationNumber;
        public FastVec2i Cord;
    }

    // We probably want to just transmit these maps as int[] blockids through the mapchunks (maybe rainheightmap suffices already?)
    // make a property block.BlockColor for the blocks color
    // and have the chunk intmap cached client side
    public class CANChunkMapLayer : CANRGBMapLayer
    {
        const int chunksize = GlobalConstants.ChunkSize;
        IWorldChunk[] chunksTmp;

        object chunksToGenLock = new object();
        UniqueQueue<FastVec2i> chunksToGen = new();
        ConcurrentDictionary<FastVec2i, CANMultiChunkMapComponent> loadedMapData = new();
        HashSet<FastVec2i> curVisibleChunks = new();

        ConcurrentQueue<CANReadyMapPiece> readyMapPieces = new ConcurrentQueue<CANReadyMapPiece>();

        public override MapLegendItem[] LegendItems => throw new NotImplementedException();
        public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
        public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
        public override string Title => "Terrain";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public override string LayerGroupCode => "terrain";

        ICoreClientAPI capi;

        bool colorAccurate;

        public string getMapDbFilePath()
        {
            string path = Path.Combine(GamePaths.DataPath, "Maps");
            GamePaths.EnsurePathExists(path);

            return Path.Combine(path, api.World.SavegameIdentifier + ".db");
        }



        public CANChunkMapLayer(ICoreAPI api, ICANWorldMapManager mapSink) : base(api, mapSink)
        {
            //api.Event.ChunkDirty += Event_OnChunkDirty;
            capi = api as ICoreClientAPI;

            if (api.Side == EnumAppSide.Client)
            {

                api.ChatCommands.GetOrCreate("map")
                    .BeginSubCommand("redraw")
                        .WithDescription("Redraw the map")
                        .HandleWith(OnMapCmdRedraw)
                    .EndSubCommand();
            }
        }

        private TextCommandResult OnMapCmdRedraw(TextCommandCallingArgs args)
        {
            foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
            {
                cmp.ActuallyDispose();
            }
            loadedMapData.Clear();

            lock (chunksToGenLock)
            {
                foreach (FastVec2i cord in curVisibleChunks)
                {
                    chunksToGen.Enqueue(cord.Copy());
                }
            }
            return TextCommandResult.Success("Redrawing map...");
        }
        public void Event_OnChunkDataReceived(Dictionary<long, HashSet<(int, int, Tile)>> NewMapTiles)
        {
            lock (chunksToGenLock)
            {
                if (!mapSink.IsOpened) return;
                foreach(var atl in NewMapTiles)
                {
                    HashSet<FastVec2i> li = new();
                    foreach (var it in atl.Value)
                    {
                        FastVec2i tmpMccoord = new FastVec2i(it.Item1 / 2, it.Item2 / 2);
                        li.UnionWith(new[]{ tmpMccoord,
                            new FastVec2i(tmpMccoord.X - 1, tmpMccoord.Y - 1),
                            new FastVec2i(tmpMccoord.X, tmpMccoord.Y - 1),
                            new FastVec2i(tmpMccoord.X + 1, tmpMccoord.Y - 1),
                            new FastVec2i(tmpMccoord.X - 1, tmpMccoord.Y),
                            new FastVec2i(tmpMccoord.X + 1, tmpMccoord.Y),
                            new FastVec2i(tmpMccoord.X - 1, tmpMccoord.Y + 1),
                            new FastVec2i(tmpMccoord.X, tmpMccoord.Y + 1),
                            new FastVec2i(tmpMccoord.X + 1, tmpMccoord.Y + 1) });                        
                            /*chunksToGen.Enqueue(tmpMccoord);
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X, tmpMccoord.Y - 1));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X - 1, tmpMccoord.Y));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X, tmpMccoord.Y + 1));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X + 1, tmpMccoord.Y + 1));*/
                    }
                    foreach (var ch in li)
                    {
                        if (loadedMapData.Remove(ch, out var chm))
                        {
                            chm.ActuallyDispose();
                        }
                    }

                    //FastVec2i tmpCoord = new FastVec2i(chunkCoord.X * 32, chunkCoord.Y * 32);

                    //if (!loadedMapData.ContainsKey(tmpMccoord) /*&& !curVisibleChunks.Contains(tmpCoord)*/) continue;
                    foreach (var ch in li)
                    {
                        chunksToGen.Enqueue(ch);
                    }
                }
                
                /*chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Y - 1));
                chunksToGen.Enqueue(new FastVec2i(chunkCoord.X - 1, chunkCoord.Y));
                chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Y + 1));
                chunksToGen.Enqueue(new FastVec2i(chunkCoord.X + 1, chunkCoord.Y + 1));*/
            }
        }

        public override void OnLoaded()
        {
            if (api.Side == EnumAppSide.Server) return;
            chunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / chunksize];
        }

        public override void OnMapOpenedClient()
        {
            if(this.atlasID != CANAntiqueAtlas.LastAtlasId)
            {
                this.loadedMapData.Clear();
            }
            colorAccurate = api.World.Config.GetAsBool("colorAccurateWorldmap", false) || capi.World.Player.Privileges.IndexOf("colorAccurateWorldmap") != -1;
            /*foreach(var it in this.loadedMapData)
            {
                it.Value.ActuallyDispose();
            }
            loadedMapData.Clear();*/
            int cx = (int)Math.Floor(capi.World.Player.Entity.Pos.X / 32);
            int cz = (int)Math.Floor(capi.World.Player.Entity.Pos.Z / 32);
            int p = 5;
            lock (chunksToGen)
            {
                for (int dx = -p; dx <= p; dx++)
                {
                    for (int dz = -p; dz <= p; dz++)
                    {
                        int chunkX = cx + dx;
                        int chunkZ = cz + dz;
                        
                        var v = new FastVec2i(chunkX, chunkZ);
                        if(loadedMapData.Remove(v, out var old))
                        {
                            chunksToGen.Enqueue(v);
                        }
                        
                    }
                }
            }
        }

        public override void OnMapClosedClient()
        {
            lock (chunksToGenLock)
            {
                chunksToGen.Clear();
            }

            curVisibleChunks.Clear();
        }

        public override void Dispose()
        {
            if (loadedMapData != null)
            {
                foreach (CANMultiChunkMapComponent cmp in loadedMapData.Values)
                {
                    cmp?.ActuallyDispose();
                }
            }

            CANMultiChunkMapComponent.DisposeStatic();

            base.Dispose();
        }

        public override void OnShutDown()
        {
            CANMultiChunkMapComponent.tmpTexture?.Dispose();
        }

        float mtThread1secAccum = 0f;
        float genAccum = 0f;

        public override void OnOffThreadTick(float dt)
        {
            genAccum += dt;
            if (genAccum < 0.1) return;
            genAccum = 0;

            int quantityToGen = chunksToGen.Count;
            var dim = CANAntiqueAtlas.ClientMapInfoData.GetDimensionData();
            CANAntiqueAtlas.ClientSeenChunksByAtlases.TryGetValue(this.atlasID, out var seenChunks);
            if(seenChunks == null)
            {
                return;
            }
            while (quantityToGen > 0)
            {
                if (mapSink.IsShuttingDown) break;

                quantityToGen--;
                FastVec2i cord;

                lock (chunksToGenLock)
                {
                    if (chunksToGen.Count == 0) break;
                    cord = chunksToGen.Dequeue();
                }

                //if (!api.World.BlockAccessor.IsValidPos(cord.X * chunksize, 1, cord.Y * chunksize)) continue;
                if(loadedMapData.ContainsKey(cord))
                {
                    continue;
                }
                var tileCoords = cord.Copy();
                tileCoords.X *= 2;
                tileCoords.Y *= 2;
                //var seen = seenChunks.GetTile(tileCoords.X, tileCoords.Y);
                FastVec2i foundPlot = new FastVec2i(tileCoords.X, tileCoords.Y);
                //if (seen)
                bool seen = false;
                {
                    for (int i = 0; i < 2; i++)
                    {
                        for(int j = 0; j < 2; j++)
                        {
                            seen = seenChunks.GetTile(foundPlot.X + i, foundPlot.Y + j);
                            if(seen)
                            {
                                foundPlot.X += i;
                                foundPlot.Y += j;
                                goto jumpHere;
                            }
                        }
                    }
                    if(!seen)
                    continue;
                }
            jumpHere:
                var tile = dim.GetTile(tileCoords.X, tileCoords.Y);
                if(tile == null)
                {
                    continue;
                }
                int[] biomes = new int[4];
                short[] variations = new short[4];
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {
                        tile = dim.GetTile(tileCoords.X + i, tileCoords.Y + j);
                        biomes[j * 2 + i] = tile?.biomeID ?? -1;
                        variations[j * 2 + i] = tile?.getVariationNumber() ?? (short)-1;
                    }


                readyMapPieces.Enqueue(new CANReadyMapPiece() { VariationNumber = variations, Biome = biomes, Cord = tileCoords });
                continue;
            }
        }



        public override void OnTick(float dt)
        {
            if (!readyMapPieces.IsEmpty)
            {
                int q = Math.Min(readyMapPieces.Count, 200);
                List<CANMultiChunkMapComponent> modified = new();
                while (q-- > 0)
                {
                    if (readyMapPieces.TryDequeue(out var mappiece))
                    {
                        FastVec2i mcord = new FastVec2i(mappiece.Cord.X / CANMultiChunkMapComponent.ChunkLen, mappiece.Cord.Y / CANMultiChunkMapComponent.ChunkLen);
                        FastVec2i baseCord = new FastVec2i(mcord.X * CANMultiChunkMapComponent.ChunkLen, mcord.Y * CANMultiChunkMapComponent.ChunkLen);

                        if (CANAntiqueAtlas.LastAtlasId == -1 || !CANAntiqueAtlas.ClientSeenChunksByAtlases.ContainsKey(CANAntiqueAtlas.LastAtlasId))
                        {
                            continue;
                        }
                        if (!loadedMapData.TryGetValue(mcord, out CANMultiChunkMapComponent mccomp))
                        {
                            loadedMapData[mcord] = mccomp = new CANMultiChunkMapComponent(api as ICoreClientAPI, baseCord);
                        }

                        mccomp.setChunk(mappiece.Cord.X - baseCord.X, mappiece.Cord.Y - baseCord.Y, mappiece);
                       // modified.Add(mccomp);
                    }
                }

               // foreach (var mccomp in modified) mccomp.FinishSetChunks();
            }

            mtThread1secAccum += dt;
            if (mtThread1secAccum > 1)
            {
                List<FastVec2i> toRemove = new List<FastVec2i>();

                foreach (var val in loadedMapData)
                {
                    CANMultiChunkMapComponent mcmp = val.Value;

                    if (!mcmp.AnyChunkSet || !mcmp.IsVisible(curVisibleChunks))
                    {
                        mcmp.TTL -= 1;

                        if (mcmp.TTL <= 0)
                        {
                            FastVec2i mccord = val.Key;
                            toRemove.Add(mccord);
                            mcmp.ActuallyDispose();
                        }
                    }
                    else
                    {
                        mcmp.TTL = CANMultiChunkMapComponent.MaxTTL;
                    }
                }

                foreach (var val in toRemove)
                {
                    loadedMapData.TryRemove(val, out _);
                }

                mtThread1secAccum = 0;
            }
        }

        public override void Render(CANGuiElementMap mapElem, float dt)
        {
            if (!Active) return;

            foreach (var val in loadedMapData)
            {
                val.Value.Render(mapElem, dt);
            }
        }

        public override void OnMouseMoveClient(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
            if (!Active) return;

            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseMove(args, mapElem, hoverText);
            }
        }

        public override void OnMouseUpClient(MouseEvent args, CANGuiElementMap mapElem)
        {
            if (!Active) return;

            foreach (var val in loadedMapData)
            {
                val.Value.OnMouseUpOnElement(args, mapElem);
            }
        }
        public override void OnViewChangedClient(List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
            foreach (var val in nowVisible)
            {
                curVisibleChunks.Add(val);
            }

            foreach (var val in nowHidden)
            {
                curVisibleChunks.Remove(val);
            }
            //HERE
            lock (chunksToGenLock)
            {
                foreach (FastVec2i cord in nowVisible)
                {
                    FastVec2i tmpMccoord = new FastVec2i(cord.X / CANMultiChunkMapComponent.ChunkLen, cord.Y / CANMultiChunkMapComponent.ChunkLen);

                    int dx = cord.X % CANMultiChunkMapComponent.ChunkLen;
                    int dz = cord.Y % CANMultiChunkMapComponent.ChunkLen;
                    if (dx < 0 || dz < 0) continue;

                    if (loadedMapData.TryGetValue(tmpMccoord, out CANMultiChunkMapComponent mcomp))
                    {
                        if (mcomp.IsChunkSet(dx, dz)) continue;
                    }

                    chunksToGen.Enqueue(cord.Copy());
                }
            }

            foreach (FastVec2i cord in nowHidden)
            {
                if (cord.X < 0 || cord.Y < 0) continue;

                FastVec2i mcord = new FastVec2i(cord.X / CANMultiChunkMapComponent.ChunkLen, cord.Y / CANMultiChunkMapComponent.ChunkLen);

                if (loadedMapData.TryGetValue(mcord, out CANMultiChunkMapComponent mc))
                {
                    mc.unsetChunk(cord.X % CANMultiChunkMapComponent.ChunkLen, cord.Y % CANMultiChunkMapComponent.ChunkLen);
                }
            }
        }
    }
}