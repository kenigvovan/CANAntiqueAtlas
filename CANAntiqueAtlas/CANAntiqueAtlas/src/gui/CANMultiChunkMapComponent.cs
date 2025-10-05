using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.gui.Map;
using CANAntiqueAtlas.src.gui.Map.TileLayer;
using CANAntiqueAtlas.src.gui.render;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public class CANMultiChunkMapComponent : CANMapComponent
    {
        public const int ChunkLen = 1;

        public static LoadedTexture tmpTexture;

        public float renderZ = 50f;

        public FastVec2i chunkCoord;

        public LoadedTexture Texture;

        private Vec3d worldPos;

        private Vec2f viewPos = new Vec2f();

        private bool[,] chunkSet = new bool[1, 1];

        private const int chunksize = 32;

        public float TTL = MaxTTL;

        public static float MaxTTL = 30f;

        public bool AnyChunkSet
        {
            get
            {
                for (int i = 0; i < 1; i++)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        if (chunkSet[i, j])
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool IsChunkSet(int dx, int dz)
        {
            if (dx < 0 || dz < 0)
            {
                return false;
            }

            return chunkSet[dx, dz];
        }

        public CANMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord)
            : base(capi)
        {
            chunkCoord = baseChunkCord;
            worldPos = new Vec3d(baseChunkCord.X * 16, 0.0, baseChunkCord.Y * 16);
        }

        public void setChunk(int dx, int dz, CANReadyMapPiece mapPieace)
        {
            if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + 2 + "]");
            }

            chunkSet[dx, dz] = true;
            FinishSetChunks(mapPieace);
        }

        public void FinishSetChunks(CANReadyMapPiece mapPieace)
        {
            if (tmpTexture == null || tmpTexture.Disposed)
            {
                tmpTexture = new LoadedTexture(capi, 0, 32, 32);
            }

            var itr = new TileRenderIterator(CANAntiqueAtlas.ClientMapInfoData.GetDimensionData(), CANAntiqueAtlas.ClientSeenChunksByAtlases[CANAntiqueAtlas.LastAtlasId]);
            
            var quas = itr.SetQuartets(chunkCoord);
            
            Texture?.Dispose();
            Texture = new LoadedTexture(capi, 0, 32, 32);

            var qua = quas[0];
            byte[] data = capi.Assets.Get("canantiqueatlas:textures/gui/tiles/nothing.png").Data;
            BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(data);
            this.capi.Render.LoadOrUpdateTextureFromRgba(bitmapExternal.Pixels, false, 0, ref this.Texture);

            var texSet = CANAntiqueAtlas.biomeTextureMap.getTextureSet(mapPieace.Biome[0]);
            
            LoadedTexture src = new(capi);

            var c = Math.Floor(mapPieace.VariationNumber[0] / (float)short.MaxValue * texSet.textures.Length);
            capi.Render.GetOrLoadTexture(texSet.textures[(int)c].ToString(), ref src);
            //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/test.png", ref src);

            int v = 0, u = 0;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];

                    u = q.getTextureU();
                    v = q.getTextureV();
                    capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * j, 8 * i);
                }
            }
            
            LoadedTexture src2 = new(capi);
            texSet = CANAntiqueAtlas.biomeTextureMap.getTextureSet(mapPieace.Biome[1]);
            c = Math.Floor(mapPieace.VariationNumber[1] / (float)short.MaxValue * texSet.textures.Length);
            capi.Render.GetOrLoadTexture(texSet.textures[(int)c].ToString(), ref src2);
            
            qua = quas[1];
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            {
                var q = qua.array[i * 2 + j];
                u = q.getTextureU();
                v = q.getTextureV();
                capi.Render.RenderTextureIntoTexture(src2, 8 * u, 8 * v, 8, 8, Texture, 8 * j + 16, 8 * i);
            }
            
            LoadedTexture src3 = new(capi);
            texSet = CANAntiqueAtlas.biomeTextureMap.getTextureSet(mapPieace.Biome[2]);
            c = Math.Floor(mapPieace.VariationNumber[2] / (float)short.MaxValue * texSet.textures.Length);
            capi.Render.GetOrLoadTexture(texSet.textures[(int)c].ToString(), ref src3);
            qua = quas[2];
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            {
                var q = qua.array[i * 2 + j];
                u = q.getTextureU();
                v = q.getTextureV();
                capi.Render.RenderTextureIntoTexture(src3, 8 * u, 8 * v, 8, 8, Texture, 8 * j, 8 * i + 16);
            }
            qua = quas[3]; 
            LoadedTexture src4 = new(capi);

            texSet = CANAntiqueAtlas.biomeTextureMap.getTextureSet(mapPieace.Biome[3]);
            c = Math.Floor(mapPieace.VariationNumber[3]/ (float)short.MaxValue * texSet.textures.Length);
            capi.Render.GetOrLoadTexture(texSet.textures[(int)c].ToString(), ref src4);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];
                    u = q.getTextureU();
                    v = q.getTextureV();
                    capi.Render.RenderTextureIntoTexture(src4, 8 * u, 8 * v, 8, 8, Texture, 8 * j + 16, 8 * i + 16);
                }
            capi.Render.BindTexture2d(Texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();
            return;
        }

        public void unsetChunk(int dx, int dz)
        {
            if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + 2 + "]");
            }

            chunkSet[dx, dz] = false;
        }

        public override void Render(CANGuiElementMap map, float dt)
        {
            map.TranslateWorldPosToViewPos(worldPos, ref viewPos);
            capi.Render.Render2DTexture(Texture.TextureId, (int)(map.Bounds.renderX + (double)viewPos.X), (int)(map.Bounds.renderY + (double)viewPos.Y), (int)((float)Texture.Width * map.ZoomLevel), (int)((float)Texture.Height * map.ZoomLevel), renderZ);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public void ActuallyDispose()
        {
            Texture.Dispose();
        }

        public bool IsVisible(HashSet<FastVec2i> curVisibleChunks)
        {
            /*if (curVisibleChunks.Contains(new FastVec2i(chunkCoord.X, chunkCoord.Y)))
            {
                return true;
            }*/
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    FastVec2i item = new FastVec2i(chunkCoord.X / 2, chunkCoord.Y / 2);
                    if (curVisibleChunks.Contains(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void DisposeStatic()
        {
            tmpTexture?.Dispose();
            tmpTexture = null;
        }
    }
}
