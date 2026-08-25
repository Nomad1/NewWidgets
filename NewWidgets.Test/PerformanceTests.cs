using System;
using System.Diagnostics;

using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    /// <summary>
    /// Property retrieval sits inside a frame budget. The owner states the target as
    /// 0.1 ms at 120 fps, so a single property read must cost well under a microsecond
    /// and a full style resolve must stay in the tens of microseconds.
    ///
    /// The cascade is deliberately partially locked and referenced in a fixed order.
    /// That is a performance design. A more textbook-correct resolver that turns
    /// microseconds into milliseconds is a regression, not an improvement.
    ///
    /// This group prints real numbers every run so a slowdown is visible even when it
    /// stays under the ceiling. The assertions are deliberately loose: they exist to
    /// catch a catastrophic change, not to fail on a noisy machine.
    /// </summary>
    internal static class PerformanceTests
    {
        private static readonly int s_warmupCount = 1000;
        private static readonly int s_readCount = 200000;
        private static readonly int s_resolveCount = 2000;

        // Loose ceilings. A healthy read is well under a microsecond and a healthy
        // resolve is tens of microseconds, so these catch a hundredfold regression
        // without failing on a busy machine.
        private static readonly double s_readCeilingMicroseconds = 10.0;
        private static readonly double s_resolveCeilingMicroseconds = 2000.0;

        public static void Register()
        {
            TestRunner.Add("Test 70: property retrieval stays inside the frame budget", Test70_RetrievalCost);
        }

        /// <summary>
        /// Builds a tree deep enough that the cascade has real work to do: a window
        /// holding a panel holding a button, each carrying classes, and the button
        /// carrying a pseudo-class state.
        /// </summary>
        private static WidgetButton BuildTree()
        {
            WidgetPanel window = new WidgetPanel(WidgetManager.GetStyle("perf70window"));
            WidgetPanel panel = new WidgetPanel(WidgetManager.GetStyle("perf70panel"));
            WidgetButton button = new WidgetButton(WidgetManager.GetStyle("perf70button"), "text");

            window.AddChild(panel);
            panel.AddChild(button);

            window.Relayout();
            panel.Relayout();
            button.Relayout();

            return button;
        }

        private static void Test70_RetrievalCost(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            controller.RegisterTestFont("perf70sprite", 10, 16);

            TestEnvironment.LoadCss(
                "@font.perf70font { --font-resource: url(\"perf70sprite\"); --font-spacing: 0; }" +
                ".perf70window { width: 600px; height: 760px; color: #112233; font-family: perf70font; }" +
                ".perf70panel { width: 400px; height: 300px; font-size: 0.6em; }" +
                ".perf70button { width: 120px; height: 40px; color: #ffffff; }" +
                ".perf70window .perf70panel .perf70button { color: #aabbcc; }" +
                ".perf70button:hover { color: #ff0000; }");

            WidgetButton button = BuildTree();

            // Warm up, so the first-call cost of reflection and JIT is not measured.
            uint sink = 0;
            for (int i = 0; i < s_warmupCount; i++)
                sink += button.GetProperty(WidgetParameterIndex.TextColor, 0u);

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < s_readCount; i++)
                sink += button.GetProperty(WidgetParameterIndex.TextColor, 0u);

            watch.Stop();

            double readMicroseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / s_readCount;

            // A full style resolve: rebuild the selector chain and re-query the collection.
            for (int i = 0; i < 50; i++)
            {
                button.InvalidateLayout();
                button.Relayout();
            }

            watch.Restart();

            for (int i = 0; i < s_resolveCount; i++)
            {
                button.InvalidateLayout();
                button.Relayout();
            }

            watch.Stop();

            double resolveMicroseconds = watch.Elapsed.TotalMilliseconds * 1000.0 / s_resolveCount;

            Console.WriteLine("    property read:  {0:F4} us over {1} reads", readMicroseconds, s_readCount);
            Console.WriteLine("    style resolve:  {0:F4} us over {1} resolves", resolveMicroseconds, s_resolveCount);
            Console.WriteLine("    reads inside a 0.1 ms frame budget: {0:F0}", 100.0 / readMicroseconds);

            context.IsTrue(readMicroseconds < s_readCeilingMicroseconds,
                "a single property read costs {0:F4} us, ceiling is {1:F1} us. The cascade lookup has become far more expensive; profile before accepting",
                readMicroseconds, s_readCeilingMicroseconds);

            context.IsTrue(resolveMicroseconds < s_resolveCeilingMicroseconds,
                "a full style resolve costs {0:F4} us, ceiling is {1:F1} us. Rebuilding the selector chain has become far more expensive; profile before accepting",
                resolveMicroseconds, s_resolveCeilingMicroseconds);

            // Keep the reads from being optimised away.
            context.IsTrue(sink != 1, "sink guard, prevents the reads being removed as dead code");
        }
    }
}
