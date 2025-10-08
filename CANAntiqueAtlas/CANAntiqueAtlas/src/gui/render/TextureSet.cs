using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;
using Vintagestory.API.Common;

namespace CANAntiqueAtlas.src.gui.render
{
    public class TextureSet: IComparable<TextureSet>
    {
        public static TextureSet TEST = new TextureSet(false, "TEST", 
                    Textures.TILE_TEST, 
                    Textures.TILE_TEST);
        public static TextureSet TEMPERATEFOREST = new TextureSet(false, "TEMPERATEFOREST", 
                    Textures.TILE_FOREST, 
                    Textures.TILE_FOREST2, 
                    Textures.TILE_FOREST3);
        public static TextureSet SNOW = new TextureSet(false, "SNOW", // you know nothing.
                    Textures.TILE_SNOW, Textures.TILE_SNOW, Textures.TILE_SNOW, Textures.TILE_SNOW, Textures.TILE_SNOW,
                    Textures.TILE_SNOW1, Textures.TILE_SNOW1, Textures.TILE_SNOW1,
                    Textures.TILE_SNOW2, Textures.TILE_SNOW2, Textures.TILE_SNOW2,
                    Textures.TILE_SNOW3, Textures.TILE_SNOW4, Textures.TILE_SNOW5, Textures.TILE_SNOW6);
        public static TextureSet SWAMP = standard("SWAMP",
                    Textures.TILE_SWAMP, Textures.TILE_SWAMP, Textures.TILE_SWAMP,
                    Textures.TILE_SWAMP2,
                    Textures.TILE_SWAMP3,
                    Textures.TILE_SWAMP4,
                    Textures.TILE_SWAMP5,
                    Textures.TILE_SWAMP6);
        public static TextureSet JUNGLE = standard("JUNGLE",
                    Textures.TILE_JUNGLE, Textures.TILE_JUNGLE2);

        public static TextureSet SNOW_PINES = standard("SNOW_PINES",
                    Textures.TILE_SNOW_PINES, Textures.TILE_SNOW_PINES2, Textures.TILE_SNOW_PINES3);
        public static TextureSet WATER = standard("WATER", 
                    Textures.TILE_WATER, Textures.TILE_WATER2);
        public static TextureSet DESERT = standard("DESERT",
                    Textures.TILE_SAND, Textures.TILE_SAND,
                    Textures.TILE_SAND2, Textures.TILE_SAND2,
                    Textures.TILE_SAND3, Textures.TILE_SAND3,
                    Textures.TILE_SAND_BUSHES, Textures.TILE_SAND_BUSHES,
                    Textures.TILE_CACTI);
        public static TextureSet PLATEAU_MESA_TREES = standard("PLATEAU_MESA_TREES",
                    Textures.TILE_PLATEAU_MESA, Textures.TILE_PLATEAU_MESA2,
                    Textures.TILE_PLATEAU_MESA_LOW, Textures.TILE_PLATEAU_MESA_LOW2,
                    Textures.TILE_PLATEAU_TREES, Textures.TILE_PLATEAU_TREES_LOW);
        public static TextureSet PLAINS = standard("PLAINS", 
                    Textures.TILE_GRASS, Textures.TILE_GRASS2, Textures.TILE_GRASS3, Textures.TILE_GRASS4);
        public static TextureSet MOUNTAINS_SNOW_CAPS = standard("MOUNTAINS_SNOW_CAPS", 
                    Textures.TILE_MOUNTAINS, Textures.TILE_SNOW_CAPS);
        public static TextureSet MOUNTAINS = standard("MOUNTAINS", // Has a few trees on top.
                    Textures.TILE_MOUNTAINS, Textures.TILE_MOUNTAINS,
                    Textures.TILE_MOUNTAINS2, Textures.TILE_MOUNTAINS2,
                    Textures.TILE_MOUNTAINS3,
                    Textures.TILE_MOUNTAINS4);
        public static TextureSet HILLS = standard("HILLS", 
                    Textures.TILE_HILLS);
        public static TextureSet SPARSE_FOREST = standard("SPARSE_FOREST", 
                    Textures.TILE_SPARSE_FOREST, Textures.TILE_SPARSE_FOREST2, Textures.TILE_SPARSE_FOREST3);
        public static TextureSet LAVA = standard("LAVA",
                    Textures.TILE_LAVA, Textures.TILE_LAVA2);
        public static TextureSet MEGA_SPRUCE = standard("MEGA_SPRUCE", 
                    Textures.TILE_MEGA_SPRUCE, Textures.TILE_MEGA_SPRUCE2);

        /** Name of the texture pack to write in the config file. */
        public string name; 
	
	    /** The actual textures in this set. */
	    public AssetLocation[] textures;
	
	    /** Texture sets that a tile rendered with this set can be stitched to,
	     * excluding itself. */
	    private HashSet<TextureSet> stitchTo = new HashSet<TextureSet>();
        private HashSet<TextureSet> stitchToHorizontal = new HashSet<TextureSet>();
        private HashSet<TextureSet> stitchToVertical = new HashSet<TextureSet>();

        /** Whether the texture set is part of the standard pack. Only true for
         * static constants in this class. */
        public bool isStandard;
	
	    private bool stitchesToNull = false;
        private bool anisotropicStitching = false;

        private static TextureSet standard(string name, params AssetLocation[] textures)
        {
            return new TextureSet(true, name, textures);
        }

        private TextureSet(bool isStandard, string name, params AssetLocation[] textures)
        {
            this.isStandard = isStandard;
            this.name = name;
            this.textures = textures;
        }
        /** Name has to be unique, it is used for equals() tests. */
        public TextureSet(string name, params AssetLocation[] textures): this(false, name, textures)
        {
            
        }

        /** Allow this texture set to be stitched to empty space, i.e. edge of the map. */
        public TextureSet StitchesToNull()
        {
            this.stitchesToNull = true;
            return this;
        }

        /** Add other texture sets that this texture set will be stitched to
         * (but the opposite may be false, in case of asymmetric stitching.) */
        public TextureSet StitchTo(params TextureSet[] textureSets)
        {
            foreach(TextureSet textureSet in textureSets)
            {
                stitchTo.Add(textureSet);
            }
            return this;
        }
        /** Same as {@link #stitchTo()}, but symmetrical. */
        public TextureSet StitchToMutual(params TextureSet[] textureSets)
        {
            foreach (TextureSet textureSet in textureSets)
            {
                stitchTo.Add(textureSet);
                textureSet.stitchTo.Add(this);
            }
            return this;
        }

        public TextureSet StitchToHorizontal(params TextureSet[] textureSets)
        {
            this.anisotropicStitching = true;
            foreach (TextureSet textureSet in textureSets)
            {
                stitchToHorizontal.Add(textureSet);
            }
            return this;
        }
        public TextureSet StitchToVertical(params TextureSet[] textureSets)
        {
            this.anisotropicStitching = true;
            foreach (TextureSet textureSet in textureSets)
            {
                stitchToVertical.Add(textureSet);
            }
            return this;
        }

        /** Actually used when stitching along the diagonal. */
        public bool shouldStitchTo(TextureSet toSet)
        {
            return toSet == this || stitchesToNull && toSet == null || stitchTo.Contains(toSet);
        }
        public bool shouldStitchToHorizontally(TextureSet toSet)
        {
            if (toSet == this || stitchesToNull && toSet == null) return true;
            if (anisotropicStitching) return stitchToHorizontal.Contains(toSet);
            else return stitchTo.Contains(toSet);
        }
        public bool shouldStitchToVertically(TextureSet toSet)
        {
            if (toSet == this || stitchesToNull && toSet == null) return true;
            if (anisotropicStitching) return stitchToVertical.Contains(toSet);
            else return stitchTo.Contains(toSet);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is TextureSet)) {
                return false;
            }
            TextureSet set = (TextureSet)obj;
            return this.name.Equals(set.name);
        }

        /** A special texture set that is stitched to everything except water. */
        private class TextureSetShore: TextureSet
        {

            private TextureSet water;
		    public TextureSetShore(string name, TextureSet water, AssetLocation[] textures): base(true, name, textures)
            {
                
                this.water = water;
            }
            public new bool shouldStitchToHorizontally(TextureSet otherSet)
            {
                return otherSet == this || !water.shouldStitchToHorizontally(otherSet);
            }
            public new bool shouldStitchToVertically(TextureSet otherSet)
            {
                return otherSet == this || !water.shouldStitchToVertically(otherSet);
            }
        }

    /** Stitch provided texture sets mutually between each other. */
    public static void stitchMutually(params TextureSet[] sets)
    {
        foreach (TextureSet set1 in sets)
        {
            foreach (TextureSet set2 in sets)
            {
                if (set1 != set2) set1.StitchTo(set2);
            }
        }
    }
    public static void stitchMutuallyHorizontally(params TextureSet[] sets)
    {
        foreach (TextureSet set1 in sets)
        {
            foreach (TextureSet set2 in sets)
            {
                if (set1 != set2) set1.StitchToHorizontal(set2);
            }
        }
    }
    public static void stitchMutuallyVertically(params TextureSet[] sets)
    {
        foreach (TextureSet set1 in sets)
        {
            foreach (TextureSet set2 in sets)
            {
                if (set1 != set2) set1.StitchToVertical(set2);
            }
        }
    }
    public int CompareTo(TextureSet other)
    {
        return string.Compare(name, other.name, StringComparison.Ordinal);
    }

}
}
