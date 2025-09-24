using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.MathTools;
using ProtoBuf;

namespace CANAntiqueAtlas.src.core
{
    /**
     * Contains information about the biome and - on the client - the variation
     * number of the biome's texture set.
     */
    [ProtoContract]
    public class Tile
    {
        [ProtoMember(1)]
        public int biomeID;

        private static Random RANDOM = new Random();

        /** Used for randomizing textures.
         * Takes on values from 0 to {@link Short#MAX_VALUE} - 1. */
        //REMOVE FROM SERIALIZATION
        [ProtoMember(2)]
        private short variationNumber;
        public Tile()
        {
        }
        public Tile(int biomeID): this(biomeID, (byte)0)
        {
            randomizeTexture();
        }
        public Tile(int biomeID, byte variationNumber)
        {
            this.biomeID = biomeID;
            this.variationNumber = variationNumber;
        }

        /** Set variation number to a random byte. */
        public void randomizeTexture()
        {
            this.variationNumber = (short)RANDOM.Next(short.MaxValue);
        }

        public short getVariationNumber()
        {
            return variationNumber;
        }
        public override string ToString()
        {
            return "tile" + biomeID;
        }

        public override bool Equals(object obj)
        {
            return (obj is Tile) && ((Tile)obj).biomeID == biomeID;
        }
    }
}
