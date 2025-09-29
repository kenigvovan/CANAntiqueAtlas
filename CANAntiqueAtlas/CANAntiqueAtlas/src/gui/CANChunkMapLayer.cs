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

namespace CANAntiqueAtlas.src.gui
{
    public class ReadyMapPiece
    {
        public int[] Pixels;
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

        ConcurrentQueue<ReadyMapPiece> readyMapPieces = new ConcurrentQueue<ReadyMapPiece>();

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
        public void Event_OnChunkDataReceived(Dictionary<int, HashSet<(int, int, Tile)>> NewMapTiles)
        {
            lock (chunksToGenLock)
            {
                if (!mapSink.IsOpened) return;
                foreach(var atl in NewMapTiles)
                {
                    foreach (var it in atl.Value)
                    {
                        FastVec2i tmpMccoord = new FastVec2i(it.Item1, it.Item2);
                        //FastVec2i tmpCoord = new FastVec2i(chunkCoord.X * 32, chunkCoord.Y * 32);

                        //if (!loadedMapData.ContainsKey(tmpMccoord) /*&& !curVisibleChunks.Contains(tmpCoord)*/) continue;

                        chunksToGen.Enqueue(tmpMccoord);
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X, tmpMccoord.Y - 1));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X - 1, tmpMccoord.Y));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X, tmpMccoord.Y + 1));
                        chunksToGen.Enqueue(new FastVec2i(tmpMccoord.X + 1, tmpMccoord.Y + 1));
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
            colorAccurate = api.World.Config.GetAsBool("colorAccurateWorldmap", false) || (capi.World.Player.Privileges.IndexOf("colorAccurateWorldmap") != -1);
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
        float diskSaveAccum = 0f;
        Dictionary<FastVec2i, MapPieceDB> toSaveList = new Dictionary<FastVec2i, MapPieceDB>();

        public override void OnOffThreadTick(float dt)
        {
            genAccum += dt;
            if (genAccum < 0.1) return;
            genAccum = 0;

            int quantityToGen = chunksToGen.Count;
            var dim = CANAntiqueAtlas.ClientMapInfoData.GetDimensionData();
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
                if(this.loadedMapData.ContainsKey(cord))
                {
                    continue;
                }
                var tile = dim.GetTile(cord.X, cord.Y);
                if(tile == null)
                {
                    continue;
                }
                readyMapPieces.Enqueue(new ReadyMapPiece() { Pixels = null, Cord = cord });
                //continue;
                IMapChunk mc = api.World.BlockAccessor.GetMapChunk(cord.X, cord.Y);
                if (mc == null)
                {
                    try
                    {
                        readyMapPieces.Enqueue(new ReadyMapPiece() { Pixels = null, Cord = cord });
                        /*MapPieceDB piece = mapdb.GetMapPiece(cord);
                        if (piece?.Pixels != null)
                        {
                            loadFromChunkPixels(cord, piece.Pixels);
                        }*/
                    }
                    catch (ProtoBuf.ProtoException)
                    {
                        api.Logger.Warning("Failed loading map db section {0}/{1}, a protobuf exception was thrown. Will ignore.", cord.X, cord.Y);
                    }
                    catch (OverflowException)
                    {
                        api.Logger.Warning("Failed loading map db section {0}/{1}, a overflow exception was thrown. Will ignore.", cord.X, cord.Y);
                    }

                    continue;
                }

                //int[] tintedPixels = GenerateChunkImage(cord, mc, colorAccurate);
               // if (tintedPixels == null)
                {
                    lock (chunksToGenLock)
                    {
                        chunksToGen.Enqueue(cord);
                    }

                    continue;
                }

               // toSaveList[cord.Copy()] = new MapPieceDB() { Pixels = tintedPixels };

                //loadFromChunkPixels(cord, tintedPixels);
            }

            if (toSaveList.Count > 100 || diskSaveAccum > 4f)
            {
                diskSaveAccum = 0;
                toSaveList.Clear();
            }
        }



        public override void OnTick(float dt)
        {
            if (!readyMapPieces.IsEmpty)
            {
                int q = Math.Min(readyMapPieces.Count, 1);
                List<CANMultiChunkMapComponent> modified = new();
                while (q-- > 0)
                {
                    if (readyMapPieces.TryDequeue(out var mappiece))
                    {
                        FastVec2i mcord = new FastVec2i(mappiece.Cord.X / CANMultiChunkMapComponent.ChunkLen, mappiece.Cord.Y / CANMultiChunkMapComponent.ChunkLen);
                        FastVec2i baseCord = new FastVec2i(mcord.X * CANMultiChunkMapComponent.ChunkLen, mcord.Y * CANMultiChunkMapComponent.ChunkLen);

                        if (!loadedMapData.TryGetValue(mcord, out CANMultiChunkMapComponent mccomp))
                        {
                            loadedMapData[mcord] = mccomp = new CANMultiChunkMapComponent(api as ICoreClientAPI, baseCord);
                        }

                        mccomp.setChunk(mappiece.Cord.X - baseCord.X, mappiece.Cord.Y - baseCord.Y);
                       // modified.Add(mccomp);
                    }
                }

               // foreach (var mccomp in modified) mccomp.FinishSetChunks();
            }

            /*mtThread1secAccum += dt;
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
                            //mcmp.ActuallyDispose();
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
            }*/
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