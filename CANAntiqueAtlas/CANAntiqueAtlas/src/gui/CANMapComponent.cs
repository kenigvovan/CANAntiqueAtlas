using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace CANAntiqueAtlas.src.gui
{
    public abstract class CANMapComponent : IDisposable
    {
        public ICoreClientAPI capi;

        public CANMapComponent(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public virtual void Render(CANGuiElementMap map, float dt)
        {
        }

        public virtual void Dispose()
        {
        }

        public virtual void OnMouseMove(MouseEvent args, CANGuiElementMap mapElem, StringBuilder hoverText)
        {
        }

        public virtual void OnMouseUpOnElement(MouseEvent args, CANGuiElementMap mapElem)
        {
        }
    }
}
