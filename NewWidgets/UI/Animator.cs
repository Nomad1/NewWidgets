using System;
using System.Collections.Generic;
using System.Numerics;

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
    
    public class Animator
    {
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

            public InterpolateAnimatorTask(object owner, AnimationKind kind, T valueFrom, T valueTo, int time, Action<float,T,T> tickCallback, Action callback)
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

        private static readonly LinkedList<BaseAnimatorTask> s_tasks = new LinkedList<BaseAnimatorTask>();
        private static long s_lastUpdate;
        private static bool s_scheduled;

        static Animator()
        {
            s_lastUpdate = WindowController.Instance.GetTime();

        }

        private static void ReSchedule()
        {
            if (!s_scheduled)
            {
                WindowController.Instance.ScheduleAction(Update, 1);
                s_scheduled = true;
            }
        }

        public static void Update()
        {
            s_scheduled = false;
            int elapsed = (int)(WindowController.Instance.GetTime() - s_lastUpdate);
            if (elapsed > 0)
            {

                LinkedListNode<BaseAnimatorTask> node = s_tasks.First;
                while (node != null)
                {
                    LinkedListNode<BaseAnimatorTask> next = node.Next;
                    if (node.Value.UpdateAnimator(elapsed))
                    {
                        s_tasks.Remove(node);
                        node.Value.Complete();   
                    }
                    node = next;
                }

                s_lastUpdate = WindowController.Instance.GetTime();
            }

            if (s_tasks.Count > 0)
                ReSchedule();
        }

        public static void RemoveAnimation(WindowObject owner, AnimationKind kind = AnimationKind.None)
        {
            LinkedListNode<BaseAnimatorTask> node = s_tasks.First;

            while (node != null)
            {
                LinkedListNode<BaseAnimatorTask> next = node.Next;

                if (node.Value.Key.Owner == owner && (kind == AnimationKind.None || node.Value.Key.Kind == kind))
                    s_tasks.Remove(node);

                node = next;
            }
        }
        
        public static void StartAnimation<T>(WindowObject owner, AnimationKind kind, T valueFrom, T valueTo, int time, Action<float, T, T> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(owner, kind);

            BaseAnimatorTask task = new InterpolateAnimatorTask<T>(owner, kind, valueFrom, valueTo, time, tick, callback);
            s_tasks.AddLast(task);

            ReSchedule();
        }

        public static void StartCustomAnimation(WindowObject owner, AnimationKind kind, object param, int time, Action<int, object> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(owner, kind);

            BaseAnimatorTask task = new CustomAnimatorTask(owner, kind, param, time, tick, callback);
            s_tasks.AddLast(task);

            ReSchedule();
        }
    }
}

