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
		public static  string GUI_ICONS = GUI + "icons/";
		public static  string GUI_TILES = GUI + "gui/tiles/";
		public static  string GUI_MARKERS = GUI + "markers/";
		public static  string GUI_SCALEBAR = GUI + "scalebar/";
	
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
        AssetLocation TILE_FOREST = tile("forest.png");
    }
}
