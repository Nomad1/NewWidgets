using System;

using AppKit;
using CoreGraphics;
using Foundation;
using NewWidgets.Sample;
using NewWidgets.Widgets;
using NewWidgets.Mac;

namespace NewWidgets.MacSample
{
    public partial class ViewController : NSViewController
    {
        private MacController m_windowController;
        private TestWindow m_window;

        private NSTimer m_updateTimer;

        private long m_lastFrameUpdate;
        private long m_lastFrameDraw;

        private float m_averageFrameUpdateTime;
        private float m_averageFrameTime;

        private GameCanvas m_canvas;


        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            m_windowController = new MacController((int)View.Frame.Width, (int)View.Frame.Height, 1.5f, 0.6f, false, "assets");
            m_windowController.OnInit += HandleOnInit;

            m_windowController.RegisterSpriteAtlas("assets/font5.bin");

            WidgetManager.LoadUI(System.IO.File.ReadAllText("assets/ui.xml"));

            const int targetFps = 60;
            m_averageFrameTime = m_averageFrameUpdateTime = 1000.0f / targetFps;
            m_lastFrameDraw = m_lastFrameUpdate = Environment.TickCount; // unprecise timer

            m_canvas = new GameCanvas();
            m_canvas.OnDraw += HandleOnDraw;
            m_canvas.OnTouch += HandleOnTouch;

            View.AddSubview(m_canvas);
            m_canvas.Frame = View.Frame;

            m_updateTimer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(1000 / 60.0), DoUpdate);
        }

        private void HandleOnInit()
        {
            m_window = new TestWindow();
            m_windowController.AddWindow(m_window);
        }

        private void DoUpdate(NSTimer timer)
        {
            if (m_windowController != null)
            {
                m_windowController.Update();
                m_window.SetFpsValue(1000.0f / m_averageFrameUpdateTime, 1000.0f / m_averageFrameTime);

                m_canvas.SetNeedsDisplayInRect(m_canvas.Frame);
            }

            UpdateUpdateFps();
        }

        private void HandleOnDraw(CGContext context)
        {
            if (m_windowController != null)
                m_windowController.Draw(context);

            UpdateDrawFps();
        }

        private void HandleOnTouch(float x, float y, bool press, bool unpress)
        {
            if (m_windowController != null)
                m_windowController.Touch(x, y, press, unpress, 0);
        }

        private void UpdateDrawFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameDraw);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameTime = (m_averageFrameTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameDraw = time;
        }

        private void UpdateUpdateFps()
        {
            long time = Environment.TickCount;

            int frameTime = (int)(time - m_lastFrameUpdate);

            // calculate average frame time for last N frames
            int diminishing = 120 - 1;

            m_averageFrameUpdateTime = (m_averageFrameUpdateTime * diminishing + frameTime) / (float)(diminishing + 1);
            m_lastFrameUpdate = time;
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }

        public override void ViewWillDisappear()
        {
            base.ViewWillDisappear();

            m_updateTimer.Dispose();
            m_updateTimer = null;
        }
    }
}
