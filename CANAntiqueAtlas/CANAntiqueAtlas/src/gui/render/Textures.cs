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
            TILE_WATER2 = tile("water2.png");
        public static AssetLocation TILE_TEST = tile("test.png");
    }
}
