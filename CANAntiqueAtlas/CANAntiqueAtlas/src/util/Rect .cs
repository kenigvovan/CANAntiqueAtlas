using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src.util
{
    [ProtoContract]
    public class Rect
    {
        [ProtoMember(1)]
        public int minX;
        [ProtoMember(2)]
        public int minY;
        [ProtoMember(3)]
        public int maxX;
        [ProtoMember(4)]
        public int maxY;

        public Rect(): this(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue)
        {
            
        }
        public Rect(int minX, int minY, int maxX, int maxY)
        {
            this.set(minX, minY, maxX, maxY);
        }
        public Rect(Rect r) : this(r.minX, r.minY, r.maxX, r.maxY)
        {

        }

        public Rect set(int minX, int minY, int maxX, int maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            return this;
        }
        public Rect set(Rect r)
        {
            this.set(r.minX, r.minY, r.maxX, r.maxY);
            return this;
        }

        /** Set minX and minY. */
        public Rect setOrigin(int x, int y)
        {
            this.minX = x;
            this.minY = y;
            return this;
        }
        /** Set maxX and maxY, assuming that minX and minY are already set. */
        public Rect setSize(int width, int height)
        {
            this.maxX = this.minX + width;
            this.maxY = this.minY + height;
            return this;
        }

        public int getWidth()
        {
            return maxX - minX;
        }
        public int getHeight()
        {
            return maxY - minY;
        }

        /** Extend the bounds to include the given point. */
        public void extendTo(int x, int y)
        {
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Rect)) return false;
            Rect r = (Rect)obj;
            return minX == r.minX && minY == r.minY && maxX == r.maxX && maxY == r.maxY;
        }

        protected Rect Clone()
        {
            return new Rect(this);
        }

        public override string ToString()
        {
            return string.Format("Rect{%d, %d, %d, %d}", minX, minY, maxX, maxY);
        }
    }
}
