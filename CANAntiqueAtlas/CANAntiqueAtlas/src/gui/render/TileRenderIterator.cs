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
    /**
     * Iterates through a tile storage for the purpose of rendering their textures.
     * Returned is an array of 4 {@link SubTile}s which constitute a whole
     * {@link Tile}.
     * The SubTile objects are generated on the fly and not retained in memory.
     * May return null!
     * @author Hunternif
     */
    public class TileRenderIterator : IEnumerable<SubTileQuartet>
    {
        private ITileStorage tiles;
	
	    /** How many chunks a tile spans. Used for viewing the map at a scale below
	     * the threshold at which the tile texture is of minimum size and no longer
	     * scales down. Can't be less than 1. */
	    private int step = 1;
        public void setStep(int step)
        {
            if (step >= 1)
            {
                this.step = step;
            }
        }

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

        /**
         * The group of adjacent tiles used for traversing the storage.
         * <pre>
         *   a
         * g d e b
         *   h i 
         *   f
         * </pre>
         * 'i' is at (x, y).
         * The returned array of subtiles represents the corner 'd-e-h-i'
         *   a b
         * c d e f
         * g h i j
         *   k l
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
        /** Current index into the grid of subtiles, starting at (-1, -1). */
        private int subtileX = -1, subtileY = -1;
        public TileRenderIterator()
        {
        }
        public TileRenderIterator(ITileStorage tiles)
        {
            this.tiles = tiles;
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
            /*
           * a b c
           * d e f
           * g h i
           */
            /*
            *   a
            * g d e b
            *   h i 
            *   f
            */
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
                if (quartet.array[0].shape == Shape.CONCAVE && ((shouldStitchTo(e, a) && quartetNum % 2 == 0) || (!(quartetNum % 2 == 0) && shouldStitchTo(e, b))))
                {
                    quartet.array[0].shape = Shape.FULL;
                }
                if (quartet.array[0].shape == Shape.CONCAVE && ((shouldStitchTo(e, d) && quartetNum == 2)))
                {
                    quartet.array[0].shape = Shape.FULL;
                }
                stitchVertically(quartet.array[1]);
                if (quartet.array[1].shape == Shape.CONCAVE && ((shouldStitchTo(e, c) && !(quartetNum % 2 == 0)) || ((quartetNum % 2 == 0) && shouldStitchTo(e, b))))
                {
                    quartet.array[1].shape = Shape.FULL;
                }
                if (quartet.array[1].shape == Shape.CONCAVE && ((shouldStitchTo(e, f) && quartetNum == 3)))
                {
                    quartet.array[1].shape = Shape.FULL;
                }
            }
            if (shouldStitchToVertically(e, h))
            {
                stitchVertically(quartet.array[2]);
                if (quartet.array[2].shape == Shape.CONCAVE && ((shouldStitchTo(e, g) && (quartetNum % 2 == 0)) || (!(quartetNum % 2 == 0) && shouldStitchTo(e, h))))
                {
                    quartet.array[2].shape = Shape.FULL;
                }
                if (quartet.array[2].shape == Shape.CONCAVE && ((shouldStitchTo(e, d) && quartetNum == 0)))
                {
                    quartet.array[2].shape = Shape.FULL;
                }
                stitchVertically(quartet.array[3]);
                if (quartet.array[3].shape == Shape.CONCAVE && ((shouldStitchTo(e, i) && !(quartetNum % 2 == 0)) || ((quartetNum % 2 == 0) && shouldStitchTo(e, h))))
                {
                    quartet.array[3].shape = Shape.FULL;
                }
                if (quartet.array[3].shape == Shape.CONCAVE && ((shouldStitchTo(e, d) && quartetNum == 1)))
                {
                    quartet.array[3].shape = Shape.FULL;
                }
            }

            // For any convex subtile check for single-object:
            /*if (quartet.array[0].shape == Shape.CONVEX && !shouldStitchToVertically(e, a) && !shouldStitchToHorizontally(e, g))
            {
                quartet.array[0].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[1].shape == Shape.CONVEX && !shouldStitchToVertically(e, b) && !shouldStitchToHorizontally(e, f))
            {
                quartet.array[1].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[2].shape == Shape.CONVEX && !shouldStitchToHorizontally(e, d) && !shouldStitchToVertically(e, h))
            {
                quartet.array[2].shape = Shape.SINGLE_OBJECT;
            }
            if (quartet.array[3].shape == Shape.CONVEX && !shouldStitchToHorizontally(e, f) && !shouldStitchToVertically(e, h))
            {
                quartet.array[3].shape = Shape.SINGLE_OBJECT;
            }*/
        }
        public SubTileQuartet[] SetQuartets(FastVec2i chunkCoord)
        {
            SubTileQuartet[] quartets = new SubTileQuartet[4] { new SubTileQuartet(), new SubTileQuartet(), new SubTileQuartet(), new SubTileQuartet() };
            chunkX = chunkCoord.X;
            chunkY = chunkCoord.Y;
            /*
             default
            a = tiles.GetTile(chunkX - 1, chunkY - 1);
            b = tiles.GetTile(chunkX, chunkY - 1);
            c = tiles.GetTile(chunkX + 1, chunkY - 1);
            d = tiles.GetTile(chunkX - 1, chunkY);
            e = tiles.GetTile(chunkX, chunkY);
            f = tiles.GetTile(chunkX + 1, chunkY);
            g = tiles.GetTile(chunkX - 1, chunkY + 1);
            h = tiles.GetTile(chunkX, chunkY + 1);
            i = tiles.GetTile(chunkX + 1, chunkY + 1);
            */
            a = tiles.GetTile(chunkX - 1, chunkY - 1);
            b = tiles.GetTile(chunkX, chunkY - 1);
            c = tiles.GetTile(chunkX + 1, chunkY - 1);
            d = tiles.GetTile(chunkX - 1, chunkY);
            e = tiles.GetTile(chunkX, chunkY);
            f = e;
            g = tiles.GetTile(chunkX - 1, chunkY + 1);
            h = e;
            i = e;

            /*
            *   a
            * g d e b
            *   h i 
            *   f
            */
            /*
            * a b c
            * d e f
            * g h i
            */
            FillQuartet(quartets[0], 0);

            a = tiles.GetTile(chunkX - 1, chunkY - 1);
            b = tiles.GetTile(chunkX, chunkY - 1);
            c = tiles.GetTile(chunkX + 1, chunkY - 1);
            e = tiles.GetTile(chunkX, chunkY);
            d = e;
           
            f = tiles.GetTile(chunkX + 1, chunkY);
            g = e;
            h = e;
            i = tiles.GetTile(chunkX + 1, chunkY + 1);
            FillQuartet(quartets[1], 1);

            a = tiles.GetTile(chunkX - 1, chunkY - 1);
            e = tiles.GetTile(chunkX, chunkY);
            b = e;
            c = e;
            d = tiles.GetTile(chunkX - 1, chunkY);
            
            f = e;
            g = tiles.GetTile(chunkX - 1, chunkY + 1);
            h = tiles.GetTile(chunkX, chunkY + 1);
            i = tiles.GetTile(chunkX + 1, chunkY + 1);
            FillQuartet(quartets[2], 2);

            e = tiles.GetTile(chunkX, chunkY);
            a = e;
            b = e;
            c = tiles.GetTile(chunkX + 1, chunkY - 1);
            d = e;
            
            f = tiles.GetTile(chunkX + 1, chunkY);
            g = tiles.GetTile(chunkX - 1, chunkY + 1);
            h = tiles.GetTile(chunkX, chunkY + 1);
            i = tiles.GetTile(chunkX + 1, chunkY + 1);
            FillQuartet(quartets[3], 3);

            return quartets;
        }
        public SubTileQuartet SetQuartet(FastVec2i chunkCoord)
        {
            chunkX = chunkCoord.X;
            chunkY = chunkCoord.Y;
            /*
             *   a
             * g d e b
             *   h i 
             *   f
             
             */
            a = tiles.GetTile(chunkX - 1, chunkY - 2);
            b = tiles.GetTile(chunkX + 1, chunkY - 1);
            f = tiles.GetTile(chunkX - 1, chunkY + 1);
            g = tiles.GetTile(chunkX - 2, chunkY - 1);
            d = tiles.GetTile(chunkX, chunkY);
            e = d;
            h = d;
            i = d;

            quartet.setCoords(subtileX, subtileY);
            _d.tile = d;
            _e.tile = e;
            _h.tile = h;
            _i.tile = i;

            // At first assume all convex:
            foreach (SubTile subtile in quartet)
            {
                subtile.shape = Shape.CONVEX;
            }

            // Connect horizontally:
            if (shouldStitchToHorizontally(d, e))
            {
                stitchHorizontally(_d);
            }
            if (shouldStitchToHorizontally(e, d))
            {
                stitchHorizontally(_e);
            }
            if (shouldStitchToHorizontally(h, i))
            {
                stitchHorizontally(_h);
            }
            if (shouldStitchToHorizontally(i, h))
            {
                stitchHorizontally(_i);
            }

            // Connect vertically:
            if (shouldStitchToVertically(d, h))
            {
                stitchVertically(_d);
                if (_d.shape == Shape.CONCAVE && shouldStitchTo(d, i))
                {
                    _d.shape = Shape.FULL;
                }
            }
            if (shouldStitchToVertically(h, d))
            {
                stitchVertically(_h);
                if (_h.shape == Shape.CONCAVE && shouldStitchTo(h, e))
                {
                    _h.shape = Shape.FULL;
                }
            }
            if (shouldStitchToVertically(e, i))
            {
                stitchVertically(_e);
                if (_e.shape == Shape.CONCAVE && shouldStitchTo(e, h))
                {
                    _e.shape = Shape.FULL;
                }
            }
            if (shouldStitchToVertically(i, e))
            {
                stitchVertically(_i);
                if (_i.shape == Shape.CONCAVE && shouldStitchTo(i, d))
                {
                    _i.shape = Shape.FULL;
                }
            }

            // For any convex subtile check for single-object:
            /*if (_d.shape == Shape.CONVEX && !shouldStitchToVertically(d, a) && !shouldStitchToHorizontally(d, g))
            {
                _d.shape = Shape.SINGLE_OBJECT;
            }
            if (_e.shape == Shape.CONVEX && !shouldStitchToVertically(e, b) && !shouldStitchToHorizontally(e, f))
            {
                _e.shape = Shape.SINGLE_OBJECT;
            }
            if (_h.shape == Shape.CONVEX && !shouldStitchToHorizontally(h, g) && !shouldStitchToVertically(h, f))
            {
                _h.shape = Shape.SINGLE_OBJECT;
            }
            if (_i.shape == Shape.CONVEX && !shouldStitchToHorizontally(i, b) && !shouldStitchToVertically(i, f))
            {
                _i.shape = Shape.SINGLE_OBJECT;
            }*/
            return quartet;
        }
        public SubTileQuartet Current
        {
            get
            {
                return SetQuartet(new FastVec2i(chunkX, chunkY));
            }
            /*get
            {
                a = b;
                b = tiles.GetTile(chunkX, chunkY - step * 2);
                c = d;
                d = e;
                e = f;
                f = tiles.GetTile(chunkX + step, chunkY - step);
                g = h;
                h = i;
                i = j;
                j = tiles.GetTile(chunkX + step, chunkY);
                k = l;
                l = tiles.GetTile(chunkX, chunkY + step);

                quartet.setCoords(subtileX, subtileY);
                _d.tile = d;
                _e.tile = e;
                _h.tile = h;
                _i.tile = i;

                // At first assume all convex:
                foreach (SubTile subtile in quartet)
                {
                    subtile.shape = Shape.CONVEX;
                }

                // Connect horizontally:
                if (shouldStitchToHorizontally(d, e))
                {
                    stitchHorizontally(_d);
                }
                if (shouldStitchToHorizontally(e, d))
                {
                    stitchHorizontally(_e);
                }
                if (shouldStitchToHorizontally(h, i))
                {
                    stitchHorizontally(_h);
                }
                if (shouldStitchToHorizontally(i, h))
                {
                    stitchHorizontally(_i);
                }

                // Connect vertically:
                if (shouldStitchToVertically(d, h))
                {
                    stitchVertically(_d);
                    if (_d.shape == Shape.CONCAVE && shouldStitchTo(d, i))
                    {
                        _d.shape = Shape.FULL;
                    }
                }
                if (shouldStitchToVertically(h, d))
                {
                    stitchVertically(_h);
                    if (_h.shape == Shape.CONCAVE && shouldStitchTo(h, e))
                    {
                        _h.shape = Shape.FULL;
                    }
                }
                if (shouldStitchToVertically(e, i))
                {
                    stitchVertically(_e);
                    if (_e.shape == Shape.CONCAVE && shouldStitchTo(e, h))
                    {
                        _e.shape = Shape.FULL;
                    }
                }
                if (shouldStitchToVertically(i, e))
                {
                    stitchVertically(_i);
                    if (_i.shape == Shape.CONCAVE && shouldStitchTo(i, d))
                    {
                        _i.shape = Shape.FULL;
                    }
                }

                // For any convex subtile check for single-object:
                if (_d.shape == Shape.CONVEX && !shouldStitchToVertically(d, a) && !shouldStitchToHorizontally(d, c))
                {
                    _d.shape = Shape.SINGLE_OBJECT;
                }
                if (_e.shape == Shape.CONVEX && !shouldStitchToVertically(e, b) && !shouldStitchToHorizontally(e, f))
                {
                    _e.shape = Shape.SINGLE_OBJECT;
                }
                if (_h.shape == Shape.CONVEX && !shouldStitchToHorizontally(h, g) && !shouldStitchToVertically(h, k))
                {
                    _h.shape = Shape.SINGLE_OBJECT;
                }
                if (_i.shape == Shape.CONVEX && !shouldStitchToHorizontally(i, j) && !shouldStitchToVertically(i, l))
                {
                    _i.shape = Shape.SINGLE_OBJECT;
                }

                chunkX += step;
                subtileX += 2;
                if (chunkX > scope.maxX + 1)
                {
                    chunkX = scope.minX;
                    subtileX = -1;
                    chunkY += step;
                    subtileY += 2;
                    a = null;
                    b = null;
                    c = null;
                    d = null;
                    e = null;
                    f = tiles.GetTile(chunkX, chunkY - step);
                    g = null;
                    h = null;
                    i = null;
                    j = tiles.GetTile(chunkX, chunkY);
                    k = null;
                    l = null;
                }
                return quartet;
            }*/
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

        public IEnumerator<SubTileQuartet> GetEnumerator() => (IEnumerator<SubTileQuartet>)new TileRenderIterator(tiles);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
