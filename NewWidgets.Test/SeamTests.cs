using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

using NewWidgets.UI;
using NewWidgets.Widgets;

using SpaceAdventure.BusinessLogic.Controls;

namespace NewWidgets.Test
{
    /// <summary>
    /// Regression coverage for two harness/library seams that were fixed together:
    /// <see cref="WidgetManager.ResetStyles"/> (Test 45) and the
    /// <see cref="TestController"/> scheduled-action queue plus its <c>AdvanceTime</c> pump
    /// (Test 46). Every class/id used here is prefixed "seam" so it cannot collide with any
    /// other test file's styles.
    /// </summary>
    internal static class SeamTests
    {
        private const float Tolerance = 0.01f;

        // Duplicated from CorpusTests.AmaltheaUiRoot (private there, and CorpusTests.cs is
        // off-limits to modify) purely to skip the DialogWindow.Show() portion of Test 46 the
        // same way CorpusTests skips its own groups when the sibling checkout is absent.
        private const string AmaltheaUiRootForSkipCheck = "/Volumes/Projects/Projects/SpaceAdventure/SpaceAdventure.Client/Resources/Shared/ui";

        // Registers Test 45. Must be called BEFORE CorpusTests.Register() in Program.cs:
        // Test 45 calls WidgetManager.ResetStyles(), which wipes the shared, process-wide
        // style collection every other group (including CorpusTests) reads from. Every group
        // registered above CorpusTests in this suite already runs before it and only ever
        // touches its own "seam"/"sty<letter>"/"c<n>"-prefixed selectors on that shared
        // collection, so clearing it here cannot invalidate an assertion those groups already
        // made. CorpusTests itself reloads its ~3300 lines of CSS from scratch in every one of
        // its groups regardless of what the collection already contained, so starting it from
        // an empty collection is safe -- and removes, rather than creates, a class/id collision
        // risk for it.
        public static void RegisterStyleResetTest()
        {
            TestRunner.Add("Test 45: style collection reset", Test45_StyleCollectionReset);
        }

        // Registers Test 46. Must be called AFTER CorpusTests.Register(): its DialogWindow.Show()
        // portion constructs a real SpaceAdventure.BusinessLogic.Controls.DialogWindow, whose
        // constructor reaches into WidgetManager for the literal styles "dialog_window" and
        // "close_image_button" and the literal font "title" -- names CorpusTests.Test40 loads
        // for real from Amalthea's own CSS. Running before CorpusTests (or via a name filter
        // that skips it) would leave those undefined and fail for a reason unrelated to what
        // this test checks; see the Directory.Exists guard below for the fallback.
        public static void RegisterSchedulingTest()
        {
            TestRunner.Add("Test 46: scheduled actions and the deterministic clock", Test46_ScheduledActionsAndDeterministicClock);
        }

        private static void Test45_StyleCollectionReset(TestContext context)
        {
            TestEnvironment.Setup();

            TestEnvironment.LoadCss(".seam45rule { width: 111px; }");

            WidgetPanel before = new WidgetPanel(WidgetManager.GetStyle("seam45rule"));
            before.Relayout();

            context.AreEqualFloat(111.0f, before.Size.X, Tolerance,
                "a rule loaded before ResetStyles should apply normally, got {0}", before.Size.X);

            WidgetManager.ResetStyles();

            WidgetPanel afterReset = new WidgetPanel(WidgetManager.GetStyle("seam45rule"));
            afterReset.Relayout();

            context.AreEqualFloat(0.0f, afterReset.Size.X, Tolerance,
                "after ResetStyles, a widget matching the same class must no longer pick up the cleared rule, got {0}", afterReset.Size.X);

            TestEnvironment.LoadCss(".seam45fresh { width: 222px; }");

            WidgetPanel freshRule = new WidgetPanel(WidgetManager.GetStyle("seam45fresh"));
            freshRule.Relayout();

            context.AreEqualFloat(222.0f, freshRule.Size.X, Tolerance,
                "a fresh rule loaded after ResetStyles should apply normally, got {0}", freshRule.Size.X);
        }

        private static void Test46_ScheduledActionsAndDeterministicClock(TestContext context)
        {
            TestController controller = TestEnvironment.Setup();

            // ---- an action must not run until time reaches its due point ----

            List<string> ran = new List<string>();
            int pendingBefore = controller.PendingActionCount;

            controller.ScheduleAction(delegate { ran.Add("solo"); }, 100);

            context.AreEqual(pendingBefore + 1, controller.PendingActionCount,
                "ScheduleAction must queue the action instead of running it immediately, got PendingActionCount == {0}", controller.PendingActionCount);
            context.AreEqual(0, ran.Count, "a scheduled action must not have run yet, got {0} run(s)", ran.Count);

            controller.AdvanceTime(50);
            context.AreEqual(0, ran.Count, "advancing time short of the due point must not run the action, got {0} run(s)", ran.Count);

            controller.AdvanceTime(50); // now 100ms since scheduling: due
            context.AreEqual(1, ran.Count, "advancing time past the due point must run the action exactly once, got {0} run(s)", ran.Count);
            context.AreEqual(pendingBefore, controller.PendingActionCount,
                "a run action must be removed from the queue, so PendingActionCount should drain back to {0}, got {1}", pendingBefore, controller.PendingActionCount);

            // ---- ordering by due time, independent of scheduling order ----

            ran.Clear();
            controller.ScheduleAction(delegate { ran.Add("later"); }, 20);
            controller.ScheduleAction(delegate { ran.Add("earlier"); }, 10);

            controller.AdvanceTime(20);
            context.AreEqual(2, ran.Count, "both actions should have run by their due time, got {0}", ran.Count);
            context.AreEqual("earlier", ran[0], "the action due earlier must run first even though it was scheduled second, got {0}", ran[0]);
            context.AreEqual("later", ran[1], "the action due later must run second, got {0}", ran[1]);

            // ---- an action scheduled from inside a running action must not recurse ----

            ran.Clear();
            int callDepth = 0;
            int maxCallDepth = 0;
            int chainRemaining = 5;
            Action chained = null;
            chained = delegate
            {
                callDepth++;
                if (callDepth > maxCallDepth)
                    maxCallDepth = callDepth;

                ran.Add(chainRemaining.ToString());
                chainRemaining--;

                if (chainRemaining > 0)
                    controller.ScheduleAction(chained, 0); // due immediately: would recurse under the old synchronous ScheduleAction

                callDepth--;
            };

            controller.ScheduleAction(chained, 0);
            controller.AdvanceTime(0);

            context.AreEqual(5, ran.Count, "a chain of 5 self-rescheduling delay-0 actions should all run within one AdvanceTime sweep, got {0}", ran.Count);
            context.AreEqual(1, maxCallDepth, "chained scheduling must drain iteratively through the queue rather than recursing through the call stack, got max call depth {0}", maxCallDepth);
            context.AreEqual(pendingBefore, controller.PendingActionCount,
                "the queue should be back to its starting depth once the whole chain has run, got {0}", controller.PendingActionCount);

            // ---- the original motivating case: a real Amalthea DialogWindow through Show() ----

            if (!Directory.Exists(AmaltheaUiRootForSkipCheck))
            {
                Console.WriteLine("    Test 46: Amalthea corpus not present at {0} -- DialogWindow.Show() portion skipped", AmaltheaUiRootForSkipCheck);
                return;
            }

            int pendingBeforeDialog = controller.PendingActionCount;

            DialogWindow dialog = DialogWindow.Show("Test 46 Title", "Test 46 body text for the recursion regression check.", "@button_ok");

            context.IsNotNull(dialog, "DialogWindow.Show should construct and return a dialog");
            context.IsTrue(controller.PendingActionCount > pendingBeforeDialog,
                "Show() should have queued its appear animation and its AddWindow call rather than running them inline, got PendingActionCount == {0}", controller.PendingActionCount);

            // 1000ms comfortably covers DialogWindow's 150ms appear animation in one sweep. If
            // ScheduleAction still ran synchronously, the line above (DialogWindow.Show) would
            // already have stack-overflowed before reaching here, since AnimationManager
            // reschedules its own Update() every "frame" through ScheduleAction.
            controller.AdvanceTime(1000);

            context.IsTrue(dialog.Controlling, "once the appear animation finishes, Show()'s completion callback should have set Controlling = true");
            context.AreEqual(pendingBeforeDialog, controller.PendingActionCount,
                "once the appear animation and the deferred AddWindow have both run, the queue should drain back to where it was before Show(), got {0}", controller.PendingActionCount);
        }
    }
}
