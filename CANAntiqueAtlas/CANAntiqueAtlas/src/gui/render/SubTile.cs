using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using CANAntiqueAtlas.src.core;

namespace CANAntiqueAtlas.src.gui.render
{
    public enum Shape
    {
        CONVEX, CONCAVE, HORIZONTAL, VERTICAL, FULL, SINGLE_OBJECT
    }
    
    public class Part
    {
        public static Part TOP_LEFT = new(0, 0);
        public static Part TOP_RIGHT = new(1, 0);
        public static Part BOTTOM_LEFT = new(0, 1);
        public static Part BOTTOM_RIGHT = new(1, 1);
        //TOP_LEFT(0, 0), TOP_RIGHT(1, 0), BOTTOM_LEFT(0, 1), BOTTOM_RIGHT(1, 1);
        /** Texture offset from a whole-tile-section to the respective part, in subtiles. */
        public int u, v;
        public Part(int u, int v)
        {
            this.u = u;
            this.v = v;
        }
    }
    /**
     * A quarter of a tile, containing the following information:
     * <ul>
     * <li><b>tile</b>, containing the texture file and the variation number</li>
     * <li><b>offset</b> from the top left corner to the appropriate sub-tile part
     * 		of the texture</li>
     * <li><b>x, y</b> coordinates of the subtile on the grid, measured in subtiles,
     * 		starting from (0,0) in the top left corner</li>
     * <li><b>shape</b> of the subtile</li>
     * <li>which <b>part</b> of the whole tile this subtile constitutes</li>
     * </ul>
     * @author Hunternif
     */
    public class SubTile
    {
        public Tile tile;
        /** coordinates of the subtile on the grid, measured in subtiles,
         * starting from (0,0) in the top left corner. */
        public int x, y;     
	    public Part part;
        public Shape shape;
        public SubTile(Part part)
        {
            this.part = part;
        }

        /** Texture offset from to the respective subtile section, in subtiles. */
        public int getTextureU()
        {
            switch (shape)
            {
                case Shape.SINGLE_OBJECT: return part.u;
                case Shape.CONCAVE: return 2 + part.u;
                case Shape.VERTICAL:
                case Shape.CONVEX: return part.u * 3;
                case Shape.HORIZONTAL:
                case Shape.FULL: return 2 - part.u;
                default: return 0;
            }
        }

        /** Texture offset from to the respective subtile section, in subtiles. */
        public int getTextureV()
        {
            switch (shape)
            {
                case Shape.SINGLE_OBJECT:
                case Shape.CONCAVE: return part.v;
                case Shape.CONVEX:
                case Shape.HORIZONTAL: return 2 + part.v * 3;
                case Shape.FULL:
                case Shape.VERTICAL: return 4 - part.v;
                default: return 0;
            }
        }
    }
}
