using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.core;
using Vintagestory.API.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.gui.render
{
    /**
     * Maps biome IDs (or pseudo IDs) to textures. <i>Not thread-safe!</i>
     * <p>If several textures are set for one ID, one will be chosen at random when
     * putting tile into Atlas.</p> 
     * @author Hunternif
     */
    public class BiomeTextureMap
    {
        private static BiomeTextureMap INSTANCE = new BiomeTextureMap();
        public static BiomeTextureMap instance()
        {
            return INSTANCE;
        }

        /** This map allows keys other than the 256 biome IDs to use for special tiles. */
        Dictionary<int, TextureSet> textureMap = new();

        public static TextureSet defaultTexture = TextureSet.FOREST;
	
	    /** Assign texture set to biome. */
	    public void setTexture(int biomeID, TextureSet textureSet)
        {
            this.textureMap[biomeID] = textureSet;
            return;
            if (textureSet == null)
            {
                if (textureMap.Remove(biomeID))
                {
                    //Log.warn("Removing old texture for biome %s", biomeID);
                    if (biomeID >= 0 && biomeID < 256)
                    {
                        //markDirty();
                    }
                }
                return;
            }
            //TODO
            /*TextureSet previous = textureMap.Add(biomeID, textureSet);
            if (biomeID >= 0 && biomeID < 256)
            {
                // The config only concerns itself with biomes 0-256.
                // If the old texture set is equal to the new one (i.e. has equal name
                // and equal texture files), then there's no need to update the config.
                if (previous == null)
                {
                    //markDirty();
                }
                else if (!previous.equals(textureSet))
                {
                    Log.warn("Overwriting texture set for biome %d", biomeID);
                    markDirty();
                }
            }*/
        }

        /** Find the most appropriate standard texture set depending on
         * BiomeDictionary types. */
        private void autoRegister(int biomeID)
        {
            if (biomeID < 0 || biomeID >= 256)
            {
                //Log.warn("Biome ID %d is out of range. Auto-registering default texture set", biomeID);
                setTexture(biomeID, defaultTexture);
                return;
            }
            setTexture(biomeID, defaultTexture);
            /*BiomeGenBase biome = BiomeGenBase.getBiome(biomeID);
            if (biome == null)
            {
                //Log.warn("Biome ID %d is null. Auto-registering default texture set", biomeID);
                setTexture(biomeID, defaultTexture);
                return;
            }
            List<Type> types = Arrays.asList(BiomeDictionary.getTypesForBiome(biome));
            // 1. Swamp
            if (types.Contains(Type.SWAMP))
            {
                if (types.Contains(Type.HILLS))
                {
                    setTexture(biomeID, SWAMP_HILLS);
                }
                else
                {
                    setTexture(biomeID, SWAMP);
                }
            }
            // 2. Water
            else if (types.Contains(Type.WATER) || types.Contains(Type.RIVER))
            {
                // Water + trees = swamp
                if (types.Contains(Type.FOREST) || types.Contains(Type.JUNGLE))
                {
                    if (types.Contains(Type.HILLS))
                    {
                        setTexture(biomeID, SWAMP_HILLS);
                    }
                    else
                    {
                        setTexture(biomeID, SWAMP);
                    }
                }
                else if (types.Contains(Type.SNOWY))
                {
                    setTexture(biomeID, ICE);
                }
                else
                {
                    setTexture(biomeID, WATER);
                }
            }
            // 3. Shore
            else if (types.Contains(Type.BEACH))
            {
                if (types.Contains(Type.MOUNTAIN))
                {
                    setTexture(biomeID, ROCK_SHORE);
                }
                else
                {
                    setTexture(biomeID, SHORE);
                }
            }
            // 4. Jungle
            else if (types.Contains(Type.JUNGLE))
            {
                if (types.Contains(Type.MOUNTAIN))
                {
                    setTexture(biomeID, JUNGLE_CLIFFS);
                }
                else if (types.Contains(Type.HILLS))
                {
                    setTexture(biomeID, JUNGLE_HILLS);
                }
                else
                {
                    setTexture(biomeID, JUNGLE);
                }
            }
            // 5. Savanna
            else if (types.Contains(Type.SAVANNA))
            {
                if (types.Contains(Type.MOUNTAIN) || types.Contains(Type.HILLS))
                {
                    setTexture(biomeID, SAVANNA_CLIFFS);
                }
                else
                {
                    setTexture(biomeID, SAVANNA);
                }
            }
            // 6. Pines
            else if (types.Contains(Type.CONIFEROUS))
            {
                if (types.Contains(Type.MOUNTAIN) || types.Contains(Type.HILLS))
                {
                    setTexture(biomeID, PINES_HILLS);
                }
                else
                {
                    setTexture(biomeID, PINES);
                }
            }
            // 7. Mesa - I suspect that by using this type people usually mean "Plateau"
            else if (types.Contains(Type.MESA))
            {
                if (types.Contains(Type.FOREST))
                {
                    setTexture(biomeID, PLATEAU_MESA_TREES);
                }
                else
                {
                    setTexture(biomeID, PLATEAU_MESA);
                }
            }
            // 8. General forest
            else if (types.Contains(Type.FOREST))
            {
                // Frozen forest automatically counts as pines:
                if (types.Contains(Type.SNOWY))
                {
                    if (types.Contains(Type.HILLS))
                    {
                        setTexture(biomeID, SNOW_PINES_HILLS);
                    }
                    else
                    {
                        setTexture(biomeID, SNOW_PINES);
                    }
                }
                else
                {
                    // Segregate by density:
                    if (types.Contains(Type.SPARSE))
                    {
                        if (types.Contains(Type.HILLS))
                        {
                            setTexture(biomeID, SPARSE_FOREST_HILLS);
                        }
                        else
                        {
                            setTexture(biomeID, SPARSE_FOREST);
                        }
                    }
                    else if (types.Contains(Type.DENSE))
                    {
                        if (types.Contains(Type.HILLS))
                        {
                            setTexture(biomeID, DENSE_FOREST_HILLS);
                        }
                        else
                        {
                            setTexture(biomeID, DENSE_FOREST);
                        }
                    }
                    else
                    {
                        if (types.Contains(Type.HILLS))
                        {
                            setTexture(biomeID, FOREST_HILLS);
                        }
                        else
                        {
                            setTexture(biomeID, FOREST);
                        }
                    }
                }
            }
            // 9. Various plains
            else if (types.Contains(Type.PLAINS) || types.Contains(Type.WASTELAND))
            {
                if (types.Contains(Type.SNOWY) || types.Contains(Type.COLD))
                {
                    if (types.Contains(Type.MOUNTAIN))
                    {
                        setTexture(biomeID, MOUNTAINS_SNOW_CAPS);
                    }
                    else if (types.Contains(Type.HILLS))
                    {
                        setTexture(biomeID, SNOW_HILLS);
                    }
                    else
                    {
                        setTexture(biomeID, SNOW);
                    }
                }
                else
                {
                    if (types.Contains(Type.HILLS) || types.Contains(Type.MOUNTAIN))
                    {
                        setTexture(biomeID, DESERT_HILLS);
                    }
                    else
                    {
                        setTexture(biomeID, DESERT);
                    }
                }
            }
            // 10. General mountains
            else if (types.Contains(Type.MOUNTAIN))
            {
                setTexture(biomeID, MOUNTAINS_NAKED);
            }
            // 11. General hills
            else if (types.Contains(Type.HILLS))
            {
                if (types.Contains(Type.SNOWY) || types.Contains(Type.COLD))
                {
                    setTexture(biomeID, SNOW_HILLS);
                }
                else if (types.Contains(Type.SANDY))
                {
                    setTexture(biomeID, DESERT_HILLS);
                }
                else
                {
                    setTexture(biomeID, HILLS);
                }
            }
            else
            {
                setTexture(biomeID, defaultTexture);
            }*/
            //Log.info("Auto-registered standard texture set for biome %d", biomeID);
        }

        /** Auto-registers the biome ID if it is not registered. */
        public void checkRegistration(int biomeID)
        {
            if (!isRegistered(biomeID))
            {
                autoRegister(biomeID);
                //markDirty();
            }
        }

        public bool isRegistered(int biomeID)
        {
            return textureMap.ContainsKey(biomeID);
        }

        /** If unknown biome, auto-registers a texture set. If null, returns default set. */
        public TextureSet getTextureSet(Tile tile)
        {
            if (tile == null) return defaultTexture;
            checkRegistration(tile.biomeID);
            textureMap.TryGetValue(tile.biomeID, out var v);
            return v;
        }

        public AssetLocation getTexture(Tile tile)
        {
            TextureSet set = getTextureSet(tile);
            int i = (int)(Math.Floor((float)(tile.getVariationNumber())
                    / (float)(short.MaxValue) * (float)(set.textures.Length)));
            return set.textures[i];
        }

        public List<AssetLocation> getAllTextures()
        {
            List<AssetLocation> list = new List<AssetLocation>(textureMap.Count());
            foreach (var entry in textureMap)
            {
                list.AddRange(entry.Value.textures);
            }
            return list;
        }
    }
}
