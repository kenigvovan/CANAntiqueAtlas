using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CANAntiqueAtlas.src.gui.elements
{
    public class CANGuiElementTwoStateImageButton : GuiElementControl
    {
        private LoadedTexture normalTexture;

        private LoadedTexture activeTexture;

        private LoadedTexture hoverTexture;

        private LoadedTexture disabledTexture;

        private ActionConsumable onClick;

        private bool isOver;

        private EnumButtonStyle buttonStyle;

        private bool active;

        private bool currentlyMouseDownOnElement;

        public bool PlaySound = true;

        public static double Padding = 2.0;

        private double textOffsetY;

        public bool Visible = true;
        public bool Hidden = false;

        public override bool Focusable => enabled;
        private static AssetLocation hideIcon;
        private static AssetLocation showIcon;
        private static AssetLocation backgroundLayer;
        private LoadedTexture hideTexture;
        private LoadedTexture showTexture;
        private LoadedTexture backgroundTexture;

        //
        // Souhrn:
        //     Creates a button with text.
        //
        // Parametry:
        //   capi:
        //     The Client API
        //
        //   text:
        //     The text of the button.
        //
        //   font:
        //     The font of the text.
        //
        //   hoverFont:
        //     The font of the text when the player is hovering over the button.
        //
        //   onClick:
        //     The event fired when the button is clicked.
        //
        //   bounds:
        //     The bounds of the button.
        //
        //   style:
        //     The style of the button.
        public CANGuiElementTwoStateImageButton(ICoreClientAPI capi, AssetLocation firstImage, AssetLocation secondImage, ActionConsumable onClick,
            ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal, bool hidden = false)
            : base(capi, bounds)
        {
            hoverTexture = new LoadedTexture(capi);
            activeTexture = new LoadedTexture(capi);
            normalTexture = new LoadedTexture(capi);
            disabledTexture = new LoadedTexture(capi);
            hideTexture = new LoadedTexture(capi);
            showTexture = new LoadedTexture(capi);
            backgroundTexture = new LoadedTexture(capi);
            buttonStyle = style;
            hideIcon = firstImage;
            showIcon = secondImage;
            backgroundLayer = new AssetLocation("canantiqueatlas:gui/greenlayer.png");
            /*if (hideIcon == null)
            {
                hideIcon = capi.Assets.Get(firstImage);
            }
            if (showIcon == null)
            {
                showIcon = capi.Assets.Get(secondImage);
            }*/
            //normalText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), font);
            //normalText.AutoBoxSize(onlyGrow: true);
            //pressedText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), hoverFont);
            this.onClick = onClick;
            this.Hidden = hidden;
        }

        //
        // Souhrn:
        //     Sets the orientation of the text both when clicked and when idle.
        //
        // Parametry:
        //   orientation:
        //     The orientation of the text.
        /*public void SetOrientation(EnumTextOrientation orientation)
        {
            normalText.orientation = orientation;
            pressedText.orientation = orientation;
        }*/

        public override void BeforeCalcBounds()
        {
            /*normalText.AutoBoxSize(onlyGrow: true);
            Bounds.fixedWidth = normalText.Bounds.fixedWidth;
            Bounds.fixedHeight = normalText.Bounds.fixedHeight;
            pressedText.Bounds = normalText.Bounds.CopyOnlySize();*/
        }

        public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
        {
            ImageSurface imageSurfaceFromAsset = GuiElement.getImageSurfaceFromAsset(api, backgroundLayer);
            SurfacePattern pattern = GuiElement.getPattern(api, backgroundLayer, scale: 0.1f);
            Context context = genContext(imageSurfaceFromAsset);
            pattern.Filter = Filter.Best;
            context.SetSource(pattern);
            ElementRoundRectangle(context, Bounds);
            context.Fill();
            context.Rectangle(Bounds.drawX, Bounds.drawY, Bounds.OuterWidth, Bounds.OuterHeight);
            context.SetSourceSurface(imageSurfaceFromAsset, (int)Bounds.drawX, (int)Bounds.drawY);
            context.FillPreserve();
            generateTexture(imageSurfaceFromAsset, ref backgroundTexture);
            context.Save();


            ImageSurface imageSurface = GuiElement.getImageSurfaceFromAsset(this.api, hideIcon, 255);
            SurfacePattern pattern1 = GuiElement.getPattern(this.api, hideIcon, true, 255, 0.1f);
            pattern1.Filter = Filter.Best;
            context.SetSource(pattern1);
            context.Rectangle(this.Bounds.drawX, this.Bounds.drawY, Bounds.OuterWidth, Bounds.OuterHeight);
            context.SetSourceSurface(imageSurface, (int)this.Bounds.drawX, (int)this.Bounds.drawY);
            context.FillPreserve();
            generateTexture(imageSurface, ref hideTexture);
            context.Restore();
            pattern1.Dispose();
            imageSurface.Dispose();

            imageSurface = GuiElement.getImageSurfaceFromAsset(this.api, showIcon, 255);
            pattern1 = GuiElement.getPattern(this.api, showIcon, true, 255, 0.1f);
            pattern1.Filter = Filter.Best;
            context.SetSource(pattern1);
            context.Rectangle(this.Bounds.drawX, this.Bounds.drawY, Bounds.OuterWidth, Bounds.OuterHeight);
            context.SetSourceSurface(imageSurface, (int)this.Bounds.drawX, (int)this.Bounds.drawY);
            context.FillPreserve();
            generateTexture(imageSurface, ref showTexture);
            context.Restore();
            pattern1.Dispose();
            imageSurface.Dispose();
            /*imageSurfaceFromAsset = GuiElement.getImageSurfaceFromAsset(api, currentTexture);
            pattern = GuiElement.getPattern(api, currentTexture, scale: 2);
            pattern.Filter = Filter.Best;
            context.SetSource(pattern);
            context.SetSourceSurface(imageSurfaceFromAsset, (int)Bounds.drawX, (int)Bounds.drawY);
            context.Fill();
            context.FillPreserve();
            generateTexture(imageSurfaceFromAsset, ref firstTexture);
           */
            context.Restore();
            pattern.Dispose();
            return;
            /*Bounds.CalcWorldBounds();
            normalText.Bounds.CalcWorldBounds();
            ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
            Context context = genContext(imageSurface);
            //ComposeButton(context, imageSurface);
            
            generateTexture(imageSurface, ref normalTexture);
            context.Clear();*/

            /*if (buttonStyle != 0)
            {
                context.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
                context.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                context.Fill();
            }

            pressedText.Bounds.fixedY += textOffsetY;
            pressedText.ComposeElements(context, imageSurface);
            pressedText.Bounds.fixedY -= textOffsetY;
            generateTexture(imageSurface, ref activeTexture);
            context.Clear();
            if (buttonStyle != 0)
            {
                context.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
                context.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                context.Fill();
            }

            generateTexture(imageSurface, ref hoverTexture);
            context.Dispose();
            imageSurface.Dispose();
            imageSurface = new ImageSurface(Format.Argb32, 2, 2);
            context = genContext(imageSurface);
            if (buttonStyle != 0)
            {
                context.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
                context.Rectangle(0.0, 0.0, 2.0, 2.0);
                context.Fill();
            }*/

            /*generateTexture(imageSurface, ref disabledTexture);
            context.Dispose();
            imageSurface.Dispose();*/
        }

        private void ComposeButton(Context ctx, ImageSurface surface)
        {
            double num = GuiElement.scaled(2.5);
            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                num = GuiElement.scaled(1.5);
            }

            if (buttonStyle != 0)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
                ctx.SetSourceRGBA(23.0 / 85.0, 52.0 / 255.0, 12.0 / 85.0, 0.8);
                ctx.Fill();
            }

            if (buttonStyle == EnumButtonStyle.MainMenu)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, num);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
            }

            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth - num, num);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
                GuiElement.Rectangle(ctx, 0.0, 0.0 + num, num, Bounds.OuterHeight - num);
                ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
                ctx.Fill();
            }

            surface.BlurPartial(2.0, 5);
            /*FontExtents fontExtents = normalText.Font.GetFontExtents();
            TextExtents textExtents = normalText.Font.GetTextExtents(normalText.GetText());
            double num2 = 0.0 - fontExtents.Ascent - textExtents.YBearing;
            textOffsetY = (num2 + (normalText.Bounds.InnerHeight + textExtents.YBearing) / 2.0) / (double)RuntimeEnv.GUIScale;
            normalText.Bounds.fixedY += textOffsetY;
            normalText.ComposeElements(ctx, surface);
            normalText.Bounds.fixedY -= textOffsetY;*/
            Bounds.CalcWorldBounds();
            if (buttonStyle == EnumButtonStyle.MainMenu)
            {
                GuiElement.Rectangle(ctx, 0.0, 0.0 + Bounds.OuterHeight - num, Bounds.OuterWidth, num);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
            }

            if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
            {
                GuiElement.Rectangle(ctx, 0.0 + num, 0.0 + Bounds.OuterHeight - num, Bounds.OuterWidth - 2.0 * num, num);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
                GuiElement.Rectangle(ctx, 0.0 + Bounds.OuterWidth - num, 0.0, num, Bounds.OuterHeight);
                ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
                ctx.Fill();
            }
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            if (Visible)
            {
                api.Render.Render2DTexturePremultipliedAlpha(backgroundTexture.TextureId, Bounds);
                if (this.Hidden)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(showTexture.TextureId, Bounds);
                }
                else
                {
                    api.Render.Render2DTexturePremultipliedAlpha(hideTexture.TextureId, Bounds);
                }
                /*api.Render.Render2DTexturePremultipliedAlpha(normalTexture.TextureId, Bounds);
                if (!enabled)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(disabledTexture.TextureId, Bounds);
                }
                else if (active || currentlyMouseDownOnElement)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds);
                }
                else if (isOver)
                {
                    api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds);
                }*/
            }
        }

        public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
        {
            if (!Visible || !base.HasFocus /*|| args.KeyCode != 49*/)
            {
                return;
            }

            args.Handled = true;
            if (enabled)
            {
                if (PlaySound)
                {
                    api.Gui.PlaySound("menubutton_press");
                }

            }
        }

        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            bool num = isOver;
            setIsOver();
            if (!num && isOver && PlaySound)
            {
                api.Gui.PlaySound("menubutton");
            }
        }

        protected void setIsOver()
        {
            isOver = Visible && enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (Visible && enabled)
            {
                base.OnMouseDownOnElement(api, args);
                currentlyMouseDownOnElement = true;
                if (PlaySound)
                {
                    api.Gui.PlaySound("menubutton_down");
                }
                this.Hidden = !this.Hidden;
                args.Handled = onClick();
                setIsOver();
            }
        }

        public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
        {
            if (Visible)
            {
                if (currentlyMouseDownOnElement && !Bounds.PointInside(args.X, args.Y) && !active && PlaySound)
                {
                    api.Gui.PlaySound("menubutton_up");
                }

                base.OnMouseUp(api, args);
                currentlyMouseDownOnElement = false;
            }
        }

        public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
        {
            if (enabled && currentlyMouseDownOnElement && Bounds.PointInside(args.X, args.Y) && (args.Button == EnumMouseButton.Left || args.Button == EnumMouseButton.Right))
            {
                //args.Handled = onClick();
            }

            currentlyMouseDownOnElement = false;
        }

        //
        // Souhrn:
        //     Sets the button as active or inactive.
        //
        // Parametry:
        //   active:
        //     Active == clickable
        public void SetActive(bool active)
        {
            this.active = active;
        }

        public override void Dispose()
        {
            base.Dispose();
            hoverTexture?.Dispose();
            activeTexture?.Dispose();
            disabledTexture?.Dispose();
            normalTexture?.Dispose();
        }
    }
}
