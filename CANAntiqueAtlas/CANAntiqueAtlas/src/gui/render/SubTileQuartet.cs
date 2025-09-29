using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.gui.render
{
    /**
     * The 4 subtiles in a corner between 4 tiles, each subtile belonging to a
     * different tile. When the tiles are positioned as follows:
     * <pre>
     *  a b
     *  c d
     * </pre>
     * then the subtiles 0-1-2-3 belong to tiles a-b-c-d respectively.
     * @author Hunternif
     */
    public class SubTileQuartet: IEnumerable<SubTile>
        {/*
	     * 0 1
	     * 2 3
	     */
        public SubTile[] array;
	
	    public SubTileQuartet(): this(new SubTile(Part.TOP_LEFT), new SubTile(Part.TOP_RIGHT),
                       new SubTile(Part.BOTTOM_LEFT), new SubTile(Part.BOTTOM_RIGHT))
        {
            
        }
        public SubTileQuartet(SubTile a, SubTile b, SubTile c, SubTile d)
        {
            array = new SubTile[] { a, b, c, d };
        }

        public SubTile get(int i)
        {
            return array[i];
        }

        /** Set the coordinates for the top left subtile, and the rest of them
         * have their coordinates updated respectively. */
        public void setCoords(int x, int y)
        {
            array[0].x = x;
            array[0].y = y;
            array[1].x = x + 1;
            array[1].y = y;
            array[2].x = x;
            array[2].y = y + 1;
            array[3].x = x + 1;
            array[3].y = y + 1;
        }
        public IEnumerator<SubTile> GetEnumerator()
        {
            foreach (var sub in array)
            {
                yield return sub;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
