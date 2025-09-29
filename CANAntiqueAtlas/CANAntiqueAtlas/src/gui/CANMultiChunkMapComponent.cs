using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private bool[,] chunkSet = new bool[1,1];

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
            worldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
        }

        public void setChunk(int dx, int dz)
        {
            /*if (dx < 0 || dx >= 1 || dz < 0 || dz >= 1)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + 2 + "]");
            }*/
            chunkSet[dx, dz] = true;
            FinishSetChunks();
        }

        public void FinishSetChunks()
        {
            if (tmpTexture == null || tmpTexture.Disposed)
            {
                tmpTexture = new LoadedTexture(capi, 0, 32, 32);
            }
            var itr = new TileRenderIterator(CANAntiqueAtlas.ClientMapInfoData.GetDimensionData());
            var quas = itr.SetQuartets(chunkCoord);
            /*if (Texture == null || Texture.Disposed)
            {
                int num = 32;
                Texture = new LoadedTexture(capi, 0, num, num);
                var tile = CANAntiqueAtlas.ClientMapInfoData.GetDimensionData().GetTile(this.chunkCoord.X, chunkCoord.Y);
       
                if(tile != null)
                {
                    if(tile.biomeID == 8)
                    {
                        //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/birch2.png", ref Texture);
                    }
                    else
                    {
                        //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/snow_hills2.png", ref Texture);
                    }
                }
                else
                {
                    var c = 3;
                }
                //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/birch2.png", ref Texture);
                //capi.Render.LoadOrUpdateTextureFromRgba(emptyPixels, linearMag: false, 0, ref Texture);
            }*/
            Texture = new LoadedTexture(capi, 0, 32, 32);
            //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/snow_hills2.png", ref Texture);
            //FrameBufferRef fb = capi.Render.CreateFrameBuffer(this.Texture);
            var qua = quas[0];
            int pp = 1;
            //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/nothing.png", ref Texture);
            byte[] data = capi.Assets.Get("canantiqueatlas:textures/gui/tiles/nothing.png").Data;
            BitmapExternal bitmapExternal = capi.Render.BitmapCreateFromPng(data);
            this.capi.Render.LoadOrUpdateTextureFromRgba(bitmapExternal.Pixels, false, 0, ref this.Texture);

            
            //capi.Render.LoadTexture(bitmapExternal, ref Texture);
            //capi.Render.GlGenerateTex2DMipmaps();
            //BitmapRef bmp = capi.Render.crea .CreateBitmapFromPng(assetData, assetData.Length);
            //int textureId = this.Platform.LoadTexture(bmp, false, 0, false);
            //LoadedTexture dstTex = new LoadedTexture(capi, 0, 32, 32);

            //capi.Render.RenderTextureIntoTexture(Texture, 0, 0, 32, 32, dstTex, 0, 0);

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];
                    LoadedTexture src = new(capi);
                    var u = q.getTextureU();
                    var v = q.getTextureV();

                    int uu = 8 * u;
                    int vv = 8 * v;
                    if(q.shape == render.Shape.CONVEX)
                    {
                        var c = 3;
                    }
                    capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/test.png", ref src);
                    Console.WriteLine(string.Format("from {0}/{1} to {2}/{3}", 8 * u, 8 * v, 8 * i, 8 * j));
                    capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * j, 8 * i);
                    /*if (pp == 1)
                        break;*/
                    //LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, LoadedTexture intoTexture, float targetX, flo
                }
               /*if (pp == 1)
                    break;*/
            }
            qua = quas[1];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];
                    LoadedTexture src = new(capi);
                    var u = q.getTextureU();
                    var v = q.getTextureV();
                    //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/nothing.png", ref Texture);
                    capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/test.png", ref src);
                    capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * j + 16, 8 * i);
                }
            qua = quas[2];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];
                    LoadedTexture src = new(capi);
                    var u = q.getTextureU();
                    var v = q.getTextureV();
                    //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/nothing.png", ref Texture);
                    capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/test.png", ref src);
                    capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * j, 8 * i + 16);
                }
            qua = quas[3];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    var q = qua.array[i * 2 + j];
                    LoadedTexture src = new(capi);
                    var u = q.getTextureU();
                    var v = q.getTextureV();
                    //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/nothing.png", ref Texture);
                    capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/test.png", ref src);
                    capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * j + 16, 8 * i + 16);
                }
            /*  for (int i = 0; i < 2; i++)
          for (int j = 0; j < 2; j++)
          {
              var q = qua.array[i * 2 + j];
              LoadedTexture src = new(capi);
              var u = q.getTextureU() == 0 ? 0 : 1;
              var v = q.getTextureV() == 0 ? 0 : 1;
              capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/nothing.png", ref Texture);
              capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/birch2.png", ref src);
              capi.Render.RenderTextureIntoTexture(src, 8 * u, 8 * v, 8, 8, Texture, 8 * i, 8 * j);
              //capi.Render.GetOrLoadTexture("canantiqueatlas:gui/tiles/birch2.png", ref Texture);
              //continue;
              if (src == null || src.Disposed || src.TextureId <= 0) continue;
      }*/

            //capi.Render.DestroyFrameBuffer(fb);
            capi.Render.BindTexture2d(Texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();
            return;
        }

        public void unsetChunk(int dx, int dz)
        {
            if (dx < 0 || dx >= 1 || dz < 0 || dz >= 1)
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
            if (curVisibleChunks.Contains(new FastVec2i(chunkCoord.X, chunkCoord.Y)))
            {
                return true;
            }
            /*for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    FastVec2i item = new FastVec2i(chunkCoord.X + i, chunkCoord.Y + j);
                    if (curVisibleChunks.Contains(item))
                    {
                        return true;
                    }
                }
            }*/

            return false;
        }

        public static void DisposeStatic()
        {
            tmpTexture?.Dispose();
            tmpTexture = null;
        }
    }
}
