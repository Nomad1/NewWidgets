using System;
using CoreGraphics;
using AppKit;

namespace NewWidgets.MacSample
{
    public class GameCanvas : NSView
    {
        public event Action<float, float, bool, bool>  OnTouch;
        public event Action<CGContext> OnDraw;

        public GameCanvas()
        {
            AcceptsTouchEvents = true;
            WantsLayer = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            if (OnDraw != null)
                OnDraw(NSGraphicsContext.CurrentContext.CGContext);
        }

        public override void MouseDown(NSEvent evt)
        {
            base.MouseDown(evt);

            CGPoint point = this.ConvertPointFromView(evt.LocationInWindow, null);

            if (OnTouch != null)
                OnTouch((float)point.X, (float)point.Y, true, false);
        }

        public override void MouseUp(NSEvent evt)
        {
            base.MouseUp(evt);

            CGPoint point = this.ConvertPointFromView(evt.LocationInWindow, null);

            if (OnTouch != null)
                OnTouch((float)point.X, (float)point.Y, false, true);
        }

        public override void MouseDragged(NSEvent evt)
        {
            base.MouseDragged(evt);

            CGPoint point = this.ConvertPointFromView(evt.LocationInWindow, null);

            if (OnTouch != null)
                OnTouch((float)point.X, (float)point.Y, false, false);
        }
    }
}

