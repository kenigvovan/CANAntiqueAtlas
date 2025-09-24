using System.Collections.Concurrent;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CANAntiqueAtlas.src.core
{
    public class AtlasDataHandler
    {
        protected static string ATLAS_DATA_PREFIX = "aAtlas_";
        private ConcurrentDictionary<string, AtlasData> atlasDataClientCache = new ConcurrentDictionary<string, AtlasData>();
        /** Loads data for the given atlas ID or creates a new one. */
        public AtlasData GetAtlasData(ItemStack stack, IWorldAccessor world)
        {
            if (stack.Item is CANItemAtlas)
            {
                return GetAtlasData(stack.Attributes.GetLong("atlasID"), world);
            }
            else
            {
                return null;
            }
        }
        protected string GetAtlasDataKey(long atlasID)
        {
            return ATLAS_DATA_PREFIX + atlasID;
        }
        /** Loads data for the given atlas or creates a new one. */
        public AtlasData GetAtlasData(long atlasID, IWorldAccessor world)
        {
            string key = GetAtlasDataKey(atlasID);
            AtlasData data = null;
            if (world.Side == EnumAppSide.Client)
            {
                // Since atlas data doesn't really belong to a single world-dimension,
                // it can be cached. This should fix #67
                atlasDataClientCache.TryGetValue(key, out data);
            }
            if (data == null)
            {
                //claims.sapi.WorldManager.SaveGame.StoreData<Dictionary<string, int>>("claimsshowchunkmsgs", tmpDict);

                //data = (AtlasData)world.loadItemData(AtlasData.class, key);
                //(world.Api as IWorldManagerAPI).SaveGame.StoreData(key, null);
                if (CANAntiqueAtlas.atlasD == null)
                {
                    data = (world.Api as ICoreServerAPI).WorldManager.SaveGame.GetData<AtlasData>(key);
                    CANAntiqueAtlas.atlasD = data;
                }
                else
                {
                    data = CANAntiqueAtlas.atlasD;
                }
                //data = null;

                if (data == null)
                {
                    data = new AtlasData(key);
                    (world.Api as ICoreServerAPI).WorldManager.SaveGame.StoreData(key, data);
                }
                else
                {
                    data.GetDimensionData().parent = data;
                }
                if (world.Side == EnumAppSide.Client)
                {
                    atlasDataClientCache.TryAdd(key, data);
                }
                ;
            }
            return data;
	    }
    }
}
