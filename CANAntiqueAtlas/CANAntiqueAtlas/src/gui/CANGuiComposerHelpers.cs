using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CANAntiqueAtlas.src.gui.elements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CANAntiqueAtlas.src.gui
{
    public static class CANGuiComposerHelpers
    {
        /// <summary>
        /// Adds a clickable button button with font CairoFont.ButtonText()
        /// </summary>
        /// <param name="composer"></param>
        /// <param name="text">The text displayed inside the button</param>
        /// <param name="onClick">Handler for when the button is clicked</param>
        /// <param name="bounds"></param>
        /// <param name="style"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static GuiComposer AddButtonWithImage(this GuiComposer composer, AssetLocation first, AssetLocation second,
                ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, string key = null, bool hidden = false)
        {
            if (!composer.Composed)
            {
                CANGuiElementTwoStateImageButton elem = new CANGuiElementTwoStateImageButton(composer.Api, first, second, onClick, bounds, style, hidden);
                //elem.SetOrientation(CairoFont.ButtonText().Orientation);
                composer.AddInteractiveElement(elem, key);
            }
            return composer;
        }
    }
}
