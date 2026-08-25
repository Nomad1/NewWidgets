using System;
using System.Numerics;

using NewWidgets.Widgets;

namespace NewWidgets.Test
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            TestEnvironment.Setup();

            RegisterTests();

            bool listOnly = false;
            string filter = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--list", StringComparison.Ordinal))
                {
                    listOnly = true;
                    continue;
                }

                if (string.Equals(args[i], "--update-baselines", StringComparison.Ordinal))
                {
                    CorpusTests.UpdateBaselines = true;
                    continue;
                }

                if (filter == null && !args[i].StartsWith("--", StringComparison.Ordinal))
                    filter = args[i];
            }

            if (listOnly)
            {
                foreach (string name in TestRunner.GetGroupNames())
                    Console.WriteLine(name);
                return 0;
            }

            return TestRunner.Run(filter);
        }

        private static void RegisterTests()
        {
            TestRunner.Add("Test 0: harness self-check", Test0_HarnessSelfCheck);

            PerformanceTests.Register();
            RenameTests.Register();
            MarginOrderTests.Register();
            CssParseTests.Register();
            SelectorTests.Register();
            CascadeTests.Register();
            WidgetStyleTests.Register();
            MarkupTests.Register();

            // Test 45 calls WidgetManager.ResetStyles(), clearing the shared style collection.
            // It must run before CorpusTests reloads its stylesheets from scratch, or its
            // reset would race the corpus's own (unrelated) results -- see the comment on
            // SeamTests.RegisterStyleResetTest for why this ordering is safe for the groups
            // above too.
            SeamTests.RegisterStyleResetTest();

            // Registered next: these groups load ~3300 lines of real game CSS into
            // WidgetManager's shared, process-wide, never-cleared style collection, which
            // could in principle collide with class/id names the groups above use for their
            // own scratch styles. Running after everything above (and immediately after the
            // Test 45 reset) means any such collision cannot affect their results.
            CorpusTests.Register();

            // Test 46 constructs a real DialogWindow through its normal Show() path, which
            // needs Amalthea's "dialog_window"/"close_image_button" styles and "title" font --
            // loaded above by CorpusTests. See the comment on SeamTests.RegisterSchedulingTest
            // for why this must run after CorpusTests.
            SeamTests.RegisterSchedulingTest();

            // Registered last, and Test 52 clears the style collection before it reads
            // Conformance/login.css: that stylesheet declares ordinary names like .window and
            // .label, and WidgetManager's collection is process-wide and merges same-named
            // rules, so anything running afterwards would see them blended with the corpus.
            ConformanceCssTests.Register();

            // Scaffolding only, to prove the KNOWN/FIXED reporting path (and the exit-code
            // exemption for known failures) actually works. Delete this once real
            // known-failure groups exist in the suite.
            TestRunner.AddKnownFailure("Test 0b: known-failure plumbing", "scaffolding: deliberately fails one assertion to exercise KNOWN/FIXED reporting", Test0b_KnownFailurePlumbing);
        }

        private static void Test0_HarnessSelfCheck(TestContext context)
        {
            context.IsTrue(true, "true must be true");

            context.AreEqualFloat(1.0f, 1.0001f, 0.01f, "AreEqualFloat should tolerate a small delta within its tolerance");

            context.Throws(typeof(InvalidOperationException), ThrowInvalidOperation, "Throws should catch the exception type it was told to expect");

            WidgetPanel panel = new WidgetPanel();
            panel.Size = new Vector2(123, 45);

            context.AreEqualFloat(123, panel.Size.X, 0.001f, "WidgetPanel.Size.X should read back what was set");
            context.AreEqualFloat(45, panel.Size.Y, 0.001f, "WidgetPanel.Size.Y should read back what was set");
        }

        private static void ThrowInvalidOperation()
        {
            throw new InvalidOperationException("expected test exception");
        }

        private static void Test0b_KnownFailurePlumbing(TestContext context)
        {
            context.IsTrue(false, "deliberate failure to exercise the KNOWN-failure reporting path");
        }
    }
}
