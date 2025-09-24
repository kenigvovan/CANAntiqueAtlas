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
        //TDOO PLACE DEFAULT
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
        bool isStandard;
	
	    private bool stitchesToNull = false;
        private bool anisotropicStitching = false;

        private static TextureSet standard(string name, AssetLocation[] textures)
        {
            return new TextureSet(true, name, textures);
        }

        private TextureSet(bool isStandard, string name, AssetLocation[] textures)
        {
            this.isStandard = isStandard;
            this.name = name;
            this.textures = textures;
        }
        /** Name has to be unique, it is used for equals() tests. */
        public TextureSet(string name, AssetLocation[] textures): this(false, name, textures)
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

        public bool Equals(object obj)
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
