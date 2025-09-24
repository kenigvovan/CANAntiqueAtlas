using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAntiqueAtlas.src
{
    public class Config
    {
        public double defaultScale = 0.5;
        public double minScale = 1/32;
        public bool doSaveBrowsingPos = true;
        public float newScanInterval = 1f;
        public int rescanRate = 4;
        public int scanRadius = 2;
        public bool forceChunkLoading = false;
        public bool doRescan = true;
    }
}
