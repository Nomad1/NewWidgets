using System;
using System.Collections.Generic;

namespace NewWidgets.Test
{
    /// <summary>
    /// One registered test group. Receives a fresh <see cref="TestContext"/> and asserts
    /// against it; a failed assertion does not abort the group.
    /// </summary>
    internal delegate void TestDelegate(TestContext context);

    /// <summary>
    /// Assert-based test context, passed to every registered <see cref="TestDelegate"/>.
    /// A failed assertion prints immediately and lets the calling group continue, so a run
    /// always reports the full picture rather than stopping at the first problem.
    /// </summary>
    internal class TestContext
    {
        private readonly string m_groupName;

        private int m_assertionCount;
        private int m_failureCount;

        public int AssertionCount
        {
            get { return m_assertionCount; }
        }

        public int FailureCount
        {
            get { return m_failureCount; }
        }

        internal TestContext(string groupName)
        {
            m_groupName = groupName;
        }

        public void IsTrue(bool condition, string message, params object[] parameters)
        {
            m_assertionCount++;

            if (!condition)
                RecordFailure(message, parameters);
        }

        public void IsFalse(bool condition, string message, params object[] parameters)
        {
            IsTrue(!condition, message, parameters);
        }

        public void AreEqual(object expected, object actual, string message, params object[] parameters)
        {
            IsTrue(object.Equals(expected, actual), message, parameters);
        }

        public void AreEqualFloat(float expected, float actual, float tolerance, string message, params object[] parameters)
        {
            IsTrue(Math.Abs(expected - actual) <= tolerance, message, parameters);
        }

        public void IsNull(object value, string message, params object[] parameters)
        {
            IsTrue(value == null, message, parameters);
        }

        public void IsNotNull(object value, string message, params object[] parameters)
        {
            IsTrue(value != null, message, parameters);
        }

        public void Throws(Type exceptionType, Action action, string message, params object[] parameters)
        {
            m_assertionCount++;

            bool threw = false;
            Exception caught = null;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                threw = true;
                caught = ex;
            }

            if (threw && exceptionType.IsInstanceOfType(caught))
                return;

            string detail;
            if (!threw)
                detail = "no exception was thrown";
            else
                detail = string.Format("expected {0} but got {1}: {2}", exceptionType.Name, caught.GetType().Name, caught.Message);

            PrintFailure(FormatMessage(message, parameters) + " (" + detail + ")");
        }

        public void DoesNotThrow(Action action, string message, params object[] parameters)
        {
            m_assertionCount++;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                PrintFailure(FormatMessage(message, parameters) + " (threw " + ex.GetType().Name + ": " + ex.Message + ")");
            }
        }

        public void Fail(string message, params object[] parameters)
        {
            m_assertionCount++;
            RecordFailure(message, parameters);
        }

        private void RecordFailure(string message, object[] parameters)
        {
            PrintFailure(FormatMessage(message, parameters));
        }

        private void PrintFailure(string formattedMessage)
        {
            m_failureCount++;
            Console.WriteLine("    assert failed [{0}]: {1}", m_groupName, formattedMessage);
        }

        private static string FormatMessage(string message, object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return message;

            return string.Format(message, parameters);
        }
    }

    /// <summary>
    /// Registers and runs assert-based test groups. No framework, no fixtures: register
    /// groups with <see cref="Add"/> or <see cref="AddKnownFailure"/>, then call <see cref="Run"/>.
    /// </summary>
    internal static class TestRunner
    {
        private struct GroupRegistration
        {
            public readonly string Name;
            public readonly TestDelegate Test;
            public readonly bool IsKnownFailure;
            public readonly string Reason;

            public GroupRegistration(string name, TestDelegate test, bool isKnownFailure, string reason)
            {
                Name = name;
                Test = test;
                IsKnownFailure = isKnownFailure;
                Reason = reason;
            }
        }

        private static readonly List<GroupRegistration> s_groups = new List<GroupRegistration>();

        public static void Add(string name, TestDelegate test)
        {
            s_groups.Add(new GroupRegistration(name, test, false, null));
        }

        public static void AddKnownFailure(string name, string reason, TestDelegate test)
        {
            s_groups.Add(new GroupRegistration(name, test, true, reason));
        }

        // Not part of the public contract other test files build against -- used only by
        // Program.cs to implement --list.
        internal static string[] GetGroupNames()
        {
            string[] names = new string[s_groups.Count];
            for (int i = 0; i < s_groups.Count; i++)
                names[i] = s_groups[i].Name;
            return names;
        }

        public static int Run(string filter)
        {
            bool hasFilter = !string.IsNullOrEmpty(filter);

            int totalGroups = 0;
            int totalAssertions = 0;
            int passedGroups = 0;
            int failedGroups = 0;
            int knownFailingGroups = 0;
            int newlyFixedGroups = 0;

            foreach (GroupRegistration group in s_groups)
            {
                if (hasFilter && group.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                totalGroups++;

                TestContext context = new TestContext(group.Name);
                bool threw = false;
                Exception caught = null;

                try
                {
                    group.Test(context);
                }
                catch (Exception ex)
                {
                    threw = true;
                    caught = ex;
                }

                if (threw)
                    Console.WriteLine("    unhandled exception [{0}]: {1}: {2}", group.Name, caught.GetType().Name, caught.Message);

                // An unhandled exception counts as one failed assertion, on top of whatever
                // the group already asserted before it threw.
                int assertionsForGroup = context.AssertionCount + (threw ? 1 : 0);
                int failuresForGroup = context.FailureCount + (threw ? 1 : 0);
                bool groupPassed = failuresForGroup == 0;

                totalAssertions += assertionsForGroup;

                if (group.IsKnownFailure)
                {
                    if (groupPassed)
                    {
                        Console.WriteLine("FIXED  {0}  ({1})  [{2} assertion(s)]", group.Name, group.Reason, assertionsForGroup);
                        newlyFixedGroups++;
                    }
                    else
                    {
                        Console.WriteLine("KNOWN  {0}  ({1})  [{2} assertion(s), {3} failed]", group.Name, group.Reason, assertionsForGroup, failuresForGroup);
                        knownFailingGroups++;
                    }
                }
                else
                {
                    if (groupPassed)
                    {
                        Console.WriteLine("PASS   {0}  [{1} assertion(s)]", group.Name, assertionsForGroup);
                        passedGroups++;
                    }
                    else
                    {
                        Console.WriteLine("FAIL   {0}  [{1} assertion(s), {2} failed]", group.Name, assertionsForGroup, failuresForGroup);
                        failedGroups++;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("groups={0} assertions={1} passed={2} failed={3} known-failing={4} newly-fixed={5}",
                totalGroups, totalAssertions, passedGroups, failedGroups, knownFailingGroups, newlyFixedGroups);

            int unexpectedResults = failedGroups + newlyFixedGroups;
            return unexpectedResults == 0 ? 0 : 1;
        }
    }
}
