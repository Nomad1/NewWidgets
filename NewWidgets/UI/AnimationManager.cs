using System;
using System.Collections.Generic;

namespace NewWidgets.UI
{
    public enum AnimationKind
    {
        None = 0,
        Position = 1,
        Rotation = 2,
        Scale = 3,
        Alpha = 4,
        Custom = 5
    }

    /// <summary>
    /// Animation manager. Operates as a Singleton. No need to call Update() directly, it is scheduled by WindowController
    /// </summary>
    public sealed class AnimationManager
    {
        private static readonly AnimationManager s_instance = new AnimationManager();

        public static AnimationManager Instance
        {
            get { return s_instance; }
        }

        private readonly LinkedList<BaseAnimatorTask> m_tasks;
        private long m_lastUpdate;
        private bool m_scheduled;

        private AnimationManager()
        {
            m_lastUpdate = WindowController.Instance.GetTime();
            m_tasks = new LinkedList<BaseAnimatorTask>();
        }

        private void ReSchedule(bool resetTimer = false)
        {
            if (!m_scheduled)
            {
                WindowController.Instance.ScheduleAction(Update, 1);
                m_scheduled = true;

                if (resetTimer)
                    m_lastUpdate = WindowController.Instance.GetTime();
            }
        }

        #region Public methods

        private void Update()
        {
            m_scheduled = false;
            int elapsed = (int)(WindowController.Instance.GetTime() - m_lastUpdate);
            if (elapsed > 0)
            {
                LinkedListNode<BaseAnimatorTask> node = m_tasks.First;
                while (node != null)
                {
                    LinkedListNode<BaseAnimatorTask> next = node.Next;
                    if (node.Value.UpdateAnimator(elapsed))
                    {
                        m_tasks.Remove(node);
                        node.Value.Complete();   
                    }
                    node = next;
                }

                m_lastUpdate = WindowController.Instance.GetTime();
            }

            if (m_tasks.Count > 0)
                ReSchedule();
        }

        public void RemoveAnimation(WindowObject owner, AnimationKind kind = AnimationKind.None)
        {
            LinkedListNode<BaseAnimatorTask> node = m_tasks.First;

            while (node != null)
            {
                if (node.Value.Key.Owner == owner && (kind == AnimationKind.None || node.Value.Key.Kind == kind))
                    m_tasks.Remove(node);

                node = node.Next;
            }
        }
        
        public void StartAnimation<T>(WindowObject owner, AnimationKind kind, T valueFrom, T valueTo, int time, Action<float, T, T> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(owner, kind);

            BaseAnimatorTask task = new InterpolateAnimatorTask<T>(owner, kind, valueFrom, valueTo, time, tick, callback);
            m_tasks.AddLast(task);

            ReSchedule(m_tasks.Count == 1);
        }

        public void StartCustomAnimation(WindowObject owner, AnimationKind kind, object param, int time, Action<int, object> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(owner, kind);

            BaseAnimatorTask task = new CustomAnimatorTask(owner, kind, param, time, tick, callback);
            m_tasks.AddLast(task);

            ReSchedule(m_tasks.Count == 1);
        }

        #endregion


        #region Helper classes

        private struct AnimationKey
        {
            public readonly object Owner;
            public readonly AnimationKind Kind;

            public AnimationKey(object owner, AnimationKind kind)
            {
                Owner = owner;
                Kind = kind;
            }
        }

        private abstract class BaseAnimatorTask
        {
            private readonly AnimationKey m_key;
            private readonly Action m_endCallback;

            private readonly int m_totalTime;
            private int m_timeLeft;

            public AnimationKey Key
            {
                get { return m_key; }
            }

            protected BaseAnimatorTask(object owner, AnimationKind kind, int time, Action endCallback)
            {
                m_key = new AnimationKey(owner, kind);
                m_timeLeft = time;
                m_totalTime = time;
                m_endCallback = endCallback;
            }

            public bool UpdateAnimator(int elapsed)
            {
                elapsed = Math.Min(elapsed, m_timeLeft);
                m_timeLeft -= elapsed;

                DoUpdate(m_totalTime - m_timeLeft, m_totalTime);

                if (m_timeLeft == 0)
                    return true;

                return false;
            }

            public void Complete()
            {
                if (m_timeLeft == 0 && m_endCallback != null)
                    m_endCallback();
            }

            protected abstract void DoUpdate(int sinceStart, int totalTime);
        }

        private class InterpolateAnimatorTask<T> : BaseAnimatorTask
        {
            private readonly T m_start;
            private readonly T m_end;
            private readonly Action<float, T, T> m_tickCallback;

            public InterpolateAnimatorTask(object owner, AnimationKind kind, T valueFrom, T valueTo, int time, Action<float, T, T> tickCallback, Action callback)
                : base(owner, kind, time, callback)
            {
                m_start = valueFrom;
                m_end = valueTo;

                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int sinceStart, int totalTime)
            {
                m_tickCallback((float)sinceStart / (float)totalTime, m_start, m_end);
            }
        }

        private class CustomAnimatorTask : BaseAnimatorTask
        {
            private readonly object m_param;
            private readonly Action<int, object> m_tickCallback;

            public CustomAnimatorTask(object owner, AnimationKind kind, object param, int time, Action<int, object> tickCallback, Action callback)
                : base(owner, kind, time, callback)
            {
                m_param = param;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int sinceStart, int totalTime)
            {
                if (m_tickCallback != null)
                    m_tickCallback(sinceStart, m_param);
            }
        }

        #endregion

    }
}

