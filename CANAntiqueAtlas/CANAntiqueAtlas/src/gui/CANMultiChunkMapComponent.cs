using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public class CANMultiChunkMapComponent : CANMapComponent
    {
        public const int ChunkLen = 3;

        public static LoadedTexture tmpTexture;

        public float renderZ = 50f;

        public FastVec2i chunkCoord;

        public LoadedTexture Texture;

        private static int[] emptyPixels;

        private Vec3d worldPos;

        private Vec2f viewPos = new Vec2f();

        private bool[,] chunkSet = new bool[3, 3];

        private const int chunksize = 32;

        public float TTL = MaxTTL;

        public static float MaxTTL = 30f;

        private int[][] pixelsToSet;

        public bool AnyChunkSet
        {
            get
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
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
            if (emptyPixels == null)
            {
                emptyPixels = new int[96 * 96];
            }
        }

        public void setChunk(int dx, int dz, int[] pixels)
        {
            if (dx < 0 || dx >= 3 || dz < 0 || dz >= 3)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + 2 + "]");
            }

            if (pixelsToSet == null)
            {
                pixelsToSet = new int[9][];
            }

            pixelsToSet[dz * 3 + dx] = pixels;
            chunkSet[dx, dz] = true;
        }

        public void FinishSetChunks()
        {
            if (pixelsToSet == null)
            {
                return;
            }

            if (tmpTexture == null || tmpTexture.Disposed)
            {
                tmpTexture = new LoadedTexture(capi, 0, 32, 32);
            }

            if (Texture == null || Texture.Disposed)
            {
                int num = 96;
                Texture = new LoadedTexture(capi, 0, num, num);
                capi.Render.LoadOrUpdateTextureFromRgba(emptyPixels, linearMag: false, 0, ref Texture);
            }

            FrameBufferRef fb = capi.Render.CreateFrameBuffer(Texture);
            for (int i = 0; i < pixelsToSet.Length; i++)
            {
                if (pixelsToSet[i] != null)
                {
                    capi.Render.LoadOrUpdateTextureFromRgba(pixelsToSet[i], linearMag: false, 0, ref tmpTexture);
                    capi.Render.GlToggleBlend(blend: false);
                    capi.Render.GLDisableDepthTest();
                    capi.Render.RenderTextureIntoFrameBuffer(0, tmpTexture, 0f, 0f, 32f, 32f, fb, 32 * (i % 3), 32 * (i / 3));
                }
            }

            capi.Render.DestroyFrameBuffer(fb);
            capi.Render.BindTexture2d(Texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();
            pixelsToSet = null;
        }

        public void unsetChunk(int dx, int dz)
        {
            if (dx < 0 || dx >= 3 || dz < 0 || dz >= 3)
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
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    FastVec2i item = new FastVec2i(chunkCoord.X + i, chunkCoord.Y + j);
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
            emptyPixels = null;
            tmpTexture = null;
        }
    }
}
