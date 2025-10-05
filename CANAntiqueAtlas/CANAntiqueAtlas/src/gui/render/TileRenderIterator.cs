using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using CANAntiqueAtlas.src.util;
using Vintagestory.API.MathTools;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.gui.render
{
    public class TileRenderIterator
    {
        private ITileStorage tiles;
        private ISeenTileStorage seenTiles;
        /** The scope of iteration. */
        private Rect scope = new Rect();
        public void setScope(int minX, int minY, int maxX, int maxY)
        {
            scope.set(minX, minY, maxX, maxY);
            chunkX = minX;
            chunkY = minY;
        }
        public void setScope(Rect scope)
        {
            this.scope.set(scope);
            chunkX = scope.minX;
            chunkY = scope.minY;
        }

        /*
         a b c 
         d e f
         g h i
         where we stand at Tile e and check around for the same type of bioms or
         which can be stitched together
         */
        private Tile a, b, c, d, e, f, g, h, i;

        /** Shortcuts for the quartet. */
        private static SubTile _d = new SubTile(Part.BOTTOM_RIGHT),
						  _e = new SubTile(Part.BOTTOM_LEFT),
						  _h = new SubTile(Part.TOP_RIGHT),
						  _i = new SubTile(Part.TOP_LEFT);
        public SubTileQuartet quartet = new SubTileQuartet(_d, _e, _h, _i);

        /** Current index into the tile storage, which presumably has every tile spanning exactly 1 chunk. */
        public int chunkX, chunkY;
        public TileRenderIterator()
        {
        }
        public TileRenderIterator(ITileStorage tiles, ISeenTileStorage seenTiles)
        {
            this.tiles = tiles;
            this.seenTiles = seenTiles;
            setScope(tiles.GetScope());
        }
        public bool MoveNext()
        {
            return chunkX >= scope.minX && chunkX <= scope.maxX + 1 &&
                   chunkY >= scope.minY && chunkY <= scope.maxY + 1;
        }
        public void FillQuartet(SubTileQuartet quartet, int quartetNum)
        {
            foreach (SubTile subtile in quartet)
            {
                subtile.shape = Shape.CONVEX;
            }
            if (shouldStitchToHorizontally(e, d))
            {
                stitchHorizontally(quartet.array[0]);
                stitchHorizontally(quartet.array[2]);
            }
            if (shouldStitchToHorizontally(e, f))
            {
                stitchHorizontally(quartet.array[1]);
                stitchHorizontally(quartet.array[3]);
            }
            // Connect vertically:
            if (shouldStitchToVertically(e, b))
            {
                stitchVertically(quartet.array[0]);
                if (quartet.array[0].shape == Shape.CONCAVE && ((shouldStitchTo(e, a))))
                {
                    quartet.array[0].shape = Shape.FULL;
                }
 
                stitchVertically(quartet.array[1]);
                if (quartet.array[1].shape == Shape.CONCAVE && ((shouldStitchTo(e, c))))
                {
                    quartet.array[1].shape = Shape.FULL;
                }

            }
            if (shouldStitchToVertically(e, h))
            {
                stitchVertically(quartet.array[2]);
                if (quartet.array[2].shape == Shape.CONCAVE && ((shouldStitchTo(e, g))))
                {//
                    quartet.array[2].shape = Shape.FULL;
                }
                stitchVertically(quartet.array[3]);
                if (quartet.array[3].shape == Shape.CONCAVE && ((shouldStitchTo(e, i))))
                {
                    //
                    quartet.array[3].shape = Shape.FULL;
                }

            }

            return;
            // For any convex subtile check for single-object:
            if (quartet.array[0].shape == Shape.CONVEX && !shouldStitchTo(e, a))
            {
                quartet.array[0].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[1].shape == Shape.CONVEX && !shouldStitchTo(e, c))
            {
                quartet.array[1].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[2].shape == Shape.CONVEX && !shouldStitchTo(e, g))
            {
                quartet.array[2].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[3].shape == Shape.CONVEX && !shouldStitchTo(e, i))
            {
                quartet.array[3].shape = Shape.SINGLE_OBJECT;
            }
        }
        public Tile GetTileIfSeen(int x, int y)
        {
            if(seenTiles.HasTileAt(x, y))
            {
                return tiles.GetTile(x, y);
            }
            return null;
        }
        public void SetPos()
        {
            a = tiles.GetTile(chunkX - 1, chunkY - 1);
            b = tiles.GetTile(chunkX, chunkY - 1);
            c = tiles.GetTile(chunkX + 1, chunkY - 1);
            d = tiles.GetTile(chunkX - 1, chunkY);
            e = tiles.GetTile(chunkX, chunkY);
            f = tiles.GetTile(chunkX + 1, chunkY);
            g = tiles.GetTile(chunkX - 1, chunkY + 1);
            h = tiles.GetTile(chunkX, chunkY + 1);
            i = tiles.GetTile(chunkX + 1, chunkY + 1);
            return;
            a = GetTileIfSeen(chunkX - 1, chunkY - 1);
            d = GetTileIfSeen(chunkX, chunkY - 1);
            g = GetTileIfSeen(chunkX + 1, chunkY - 1);
            b = GetTileIfSeen(chunkX - 1, chunkY);
            e = GetTileIfSeen(chunkX, chunkY);
            h = GetTileIfSeen(chunkX + 1, chunkY);
            c = GetTileIfSeen(chunkX - 1, chunkY + 1);
            f = GetTileIfSeen(chunkX, chunkY + 1);
            i = GetTileIfSeen(chunkX + 1, chunkY + 1);
        }
        public SubTileQuartet[] SetQuartets(FastVec2i chunkCoord)
        {
            SubTileQuartet[] quartets = new SubTileQuartet[4] { new SubTileQuartet(), new SubTileQuartet(), new SubTileQuartet(), new SubTileQuartet() };
            chunkX = chunkCoord.X;
            chunkY = chunkCoord.Y;
            SetPos();

            FillQuartet(quartets[0], 0);
                /*
                * a b   c
                * f d e g
                *   h i 
                * k l   m
                */
                /*
                *   a
                * g d e b
                *   h i 
                *   f
                */
                /*
                 1 2
                 3 4

                 */
                /*
                * a b c
                * d e f
                * g h i
                */
            chunkX++;
            SetPos();
            FillQuartet(quartets[1], 1);
            


            chunkX--;
            chunkY++;
            SetPos();

            FillQuartet(quartets[2], 2);
            
            chunkX++;
            //chunkY++;
            SetPos();

            FillQuartet(quartets[3], 3);
            return quartets;
        }
        /** Whether the first tile should be stitched to the 2nd (in any direction)
         * (but the opposite is not always true!) */
        private static bool shouldStitchTo(Tile tile, Tile to)
        {
            if (tile == null || to == null) return false;
            TextureSet set = BiomeTextureMap.instance().getTextureSet(tile);
            TextureSet toSet = BiomeTextureMap.instance().getTextureSet(to);
            return set == null ? false : set.shouldStitchTo(toSet);
        }
        /** Whether the first tile should be stitched to the 2nd along the X axis
         * (but the opposite is not always true!) */
        private static bool shouldStitchToHorizontally(Tile tile, Tile to)
        {
            if (tile == null || to == null) return false;
            TextureSet set = BiomeTextureMap.instance().getTextureSet(tile);
            TextureSet toSet = BiomeTextureMap.instance().getTextureSet(to);
            return set == null ? false : set.shouldStitchToHorizontally(toSet);
        }
        /** Whether the first tile should be stitched to the 2nd along the Z axis
         * (but the opposite is not always true!) */
        private static bool shouldStitchToVertically(Tile tile, Tile to)
        {
            if (tile == null || to == null) return false;
            TextureSet set = BiomeTextureMap.instance().getTextureSet(tile);
            TextureSet toSet = BiomeTextureMap.instance().getTextureSet(to);
            return set == null ? false : set.shouldStitchToVertically(toSet);
        }

        /** Change the shape of the subtile in order to stitch it vertically
         * to another subtile. It doesn't matter if it's top or bottom. */
        private static void stitchVertically(SubTile subtile)
        {
            if (subtile.shape == Shape.HORIZONTAL) subtile.shape = Shape.CONCAVE;
            if (subtile.shape == Shape.CONVEX) subtile.shape = Shape.VERTICAL;
        }
        /** Change the shape of the subtile in order to stitch it horizontally
         * to another subtile. It doesn't matter if it's left or right. */
        private static void stitchHorizontally(SubTile subtile)
        {
            if (subtile.shape == Shape.VERTICAL) subtile.shape = Shape.CONCAVE;
            if (subtile.shape == Shape.CONVEX) subtile.shape = Shape.HORIZONTAL;
        }
        public void remove()
        {
            throw new NotImplementedException();
                //UnsupportedOperationException("cannot remove subtiles from tile storage");
        }
    }
}
