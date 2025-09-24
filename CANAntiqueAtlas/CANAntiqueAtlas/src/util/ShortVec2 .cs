using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ProtoBuf;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.util
{
    [ProtoContract]
    public class ShortVec2
    {
        [ProtoMember(1)]
        public short x;
        [ProtoMember(2)]
        public short y;
        public ShortVec2()
        {

        }
        public ShortVec2(ShortVec2 vec): this(vec.x, vec.y)
        {
            
        }

        public ShortVec2(short x, short y)
        {
            this.x = x;
            this.y = y;
        }

        public ShortVec2(int x, int y)
        {
            this.x = (short)x;
            this.y = (short)y;
        }

        public ShortVec2(double x, double y)
        {
            this.x = (short)Math.Floor(x);
            this.y = (short)Math.Floor(y);
        }

        /** Modifies and returns self. */
        public ShortVec2 add(int dx, int dy)
        {
            this.x += (short)dx;
            this.y += (short)dy;
            return this;
        }

        /** Modifies and returns self. */
        public ShortVec2 set(int x, int y)
        {
            this.x = (short)x;
            this.y = (short)y;
            return this;
        }
        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }

        public ShortVec2 Clone()
        {
            return new ShortVec2(x, y);
        }

        public double distanceTo(ShortVec2 intVec2)
        {
            double x1 = x;
            double y1 = y;
            double x2 = intVec2.x;
            double y2 = intVec2.y;
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }
        public override bool Equals(object obj)
        {
            if (!(obj is ShortVec2))

            return false;
            ShortVec2 vec = (ShortVec2)obj;
            return vec.x == x && vec.y == y;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public bool equalsIntVec3(ShortVec2 vec)
        {
            return vec.x == x && vec.y == y;
        }
    }
}
