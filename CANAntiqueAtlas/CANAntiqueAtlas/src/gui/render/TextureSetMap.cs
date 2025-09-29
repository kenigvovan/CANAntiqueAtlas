using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.gui.render
{
    public class TextureSetMap
    {
        private static TextureSetMap INSTANCE = new TextureSetMap();
        public static TextureSetMap instance()
        {
            return INSTANCE;
        }

        private Dictionary<string, TextureSet> map = new();

        public void register(TextureSet set)
        {
            //TODO
            //TextureSet old = 
                map.Add(set.name, set);
            // If the old texture set is equal to the new one (i.e. has equal name
            // and equal texture files), then there's no need to update the config.
            /*if (!set.equals(old))
            {
                markDirty();
            }*/
        }


        public TextureSet getByName(string name)
        {
            map.TryGetValue(name, out var v);
            return v;
        }

        /** If the specified name is not registered, returns a "TEST" texture set. */
        public TextureSet getByNameNonNull(string name)
        {
            TextureSet set = getByName(name);
            return set == null ? TextureSet.TEST : set;
        }

        public bool isRegistered(string name)
        {
            return map.ContainsKey(name);
        }

        public ICollection<TextureSet> getAllTextureSets()
        {
            return map.Values;
        }
        /** Returns all registered texture sets that are not part of the standard
         * pack (static constants in {@link TextureSet})/ */
        public ICollection<TextureSet> getAllNonStandardTextureSets()
        {
            List<TextureSet> list = new List<TextureSet>(map.Count());
            foreach (TextureSet set in map.Values)
            {
                if (!set.isStandard)
                {
                    list.Add(set);
                }
            }
            return list;
        }
    }
}
