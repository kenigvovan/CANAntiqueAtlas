using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Server;

namespace CANAntiqueAtlas.src.events
{
    public static class ServerEvents
    {
        public static void AddEvents(ICoreServerAPI sapi)
        {
            //sapi.Event.PlayerJoin += Event_OnPlayerJoin;
        }
        public static void Event_OnPlayerJoin(IServerPlayer player)
        {
            //ExtTileIdMap.instance().syncOnPlayer(event.player);
            //CANAntiqueAtlas.atlasData.GetAtlasData
            //data.syncOnPlayer(event.player);
        }
    }
}
