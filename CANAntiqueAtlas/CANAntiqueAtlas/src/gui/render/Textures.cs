using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace CANAntiqueAtlas.src.gui.render
{
    public class Textures
    {
		public static  string GUI = "";
		public static  string GUI_ICONS = GUI + "canantiqueatlas:icons/";
		public static  string GUI_TILES = GUI + "canantiqueatlas:gui/tiles/";
		public static  string GUI_MARKERS = GUI + "canantiqueatlas:markers/";
		public static  string GUI_SCALEBAR = GUI + "canantiqueatlas:scalebar/";
	
		public static AssetLocation gui(string path)
        {
            return new AssetLocation(GUI + path);
        }
        public static AssetLocation scaleBar(string fileName)
        {
            return new AssetLocation(GUI_SCALEBAR + fileName);
        }
        public static AssetLocation marker(string fileName)
        {
            return new AssetLocation(GUI_MARKERS + fileName);
        }
        public static AssetLocation tile(string fileName)
        {
            return new AssetLocation(GUI_TILES + fileName);
        }
        public static AssetLocation icon(string fileName)
        {
            return new AssetLocation(GUI_ICONS + fileName);
        }
        AssetLocation BOOK = gui("book.png"),
		    EXPORTED_BG = gui("exportedBG.png"),
		    BOOK_FRAME = gui("bookFrame.png"),
		    BTN_ARROWS = gui("navigateArrows.png"),
		    BTN_POSITION = gui("position.png"),
		    BOOKMARKS = gui("bookmarks.png"),
		    PLAYER = gui("player.png"),
		    SCROLLBAR_HOR = gui("scrollbar_hor.png"),
		    SCROLLBAR_VER = gui("scrollbar_ver.png"),
		    MARKER_FRAME_ON = gui("marker_frame_on.png");
        public static AssetLocation TILE_FOREST = tile("forest.png"),
            TILE_FOREST2 = tile("forest2.png"),
            TILE_FOREST3 = tile("forest3.png");
        public static AssetLocation TILE_SNOW = tile("snow.png"),
            TILE_SNOW1 = tile("snow1.png"),
            TILE_SNOW2 = tile("snow2.png"),
            TILE_SNOW3 = tile("snow3.png"),
            TILE_SNOW4 = tile("snow4.png"),
            TILE_SNOW5 = tile("snow5.png"),
            TILE_SNOW6 = tile("snow6.png"),
            TILE_WATER = tile("water.png"),
            TILE_WATER2 = tile("water2.png"),
            TILE_SWAMP = tile("swamp.png"),
            TILE_SWAMP2 = tile("swamp2.png"),
            TILE_SWAMP3 = tile("swamp3.png"),
            TILE_SWAMP4 = tile("swamp4.png"),
            TILE_SWAMP5 = tile("swamp5.png"),
            TILE_SWAMP6 = tile("swamp6.png"),
            TILE_JUNGLE = tile("jungle.png"),
	        TILE_JUNGLE2 = tile("jungle2.png"),
            TILE_SNOW_PINES = tile("snow_pines.png"),
            TILE_SNOW_PINES2 = tile("snow_pines2.png"),
            TILE_SNOW_PINES3 = tile("snow_pines3.png"),
            TILE_SAND = tile("sand.png"),
            TILE_SAND2 = tile("sand2.png"),
            TILE_SAND3 = tile("sand3.png"),
            TILE_CACTI = tile("cacti.png"),
            TILE_SAND_BUSHES = tile("sand_bushes.png"),
            TILE_GRASS = tile("grass.png"),
	        TILE_GRASS2 = tile("grass2.png"),
	        TILE_GRASS3 = tile("grass3.png"),
	        TILE_GRASS4 = tile("grass4.png"),
            TILE_PLATEAU_TREES = tile("plateau_trees.png"),
            TILE_PLATEAU_TREES_LOW = tile("plateau_trees_low.png"),
            TILE_PLATEAU_MESA = tile("plateau_mesa.png"),
            TILE_PLATEAU_MESA2 = tile("plateau_mesa2.png"),
            TILE_PLATEAU_MESA_LOW = tile("plateau_mesa_low.png"),
            TILE_PLATEAU_MESA_LOW2 = tile("plateau_mesa_low2.png"),
            TILE_MOUNTAINS = tile("mountains.png"),
            TILE_MOUNTAINS2 = tile("mountains2.png"),
            TILE_MOUNTAINS3 = tile("mountains3.png"),
            TILE_MOUNTAINS4 = tile("mountains4.png"),
            TILE_SNOW_CAPS = tile("snow_caps.png"),
            TILE_SPARSE_FOREST = tile("forest_sparse.png"),
            TILE_SPARSE_FOREST2 = tile("forest_sparse2.png"),
            TILE_SPARSE_FOREST3 = tile("forest_sparse3.png"),
            TILE_HILLS = tile("hills.png"),
            TILE_HILLS_BUSHES = tile("hills_bushes.png"),
            TILE_HILLS_CACTI = tile("hills_cacti.png"),
            TILE_HILLS_GRASS = tile("hills_grass.png"),
            TILE_LAVA = tile("lava.png"),
            TILE_LAVA2 = tile("lava2.png"),
            TILE_MEGA_SPRUCE = tile("mega_spruce.png"),
            TILE_MEGA_SPRUCE2 = tile("mega_spruce2.png")

            ;
        public static AssetLocation TILE_TEST = tile("test.png");
    }
}
