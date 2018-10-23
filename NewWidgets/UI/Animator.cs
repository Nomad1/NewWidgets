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
        private abstract class BaseAnimatorTask
        {
            private readonly AnimationKind m_kind;
            private readonly Action m_callback;
            
            private int m_timeLeft;
            private int m_totalTime;
            
            public AnimationKind Kind
            {
                get { return m_kind; }
            }

            protected BaseAnimatorTask(AnimationKind kind, int time, Action callback)
            {
                m_kind = kind;
                m_timeLeft = time;
                m_callback = callback;
                m_totalTime = 0;
            }

            public bool Update(int elapsed)
            {
                elapsed = Math.Min(elapsed, m_timeLeft);
                m_timeLeft -= elapsed;
                m_totalTime += elapsed;

                DoUpdate(elapsed, m_totalTime);

                if (m_timeLeft == 0)
                    return true;
                
                return false;
            }

            public void Complete()
            {
                if (m_timeLeft == 0 && m_callback != null)
                    m_callback();
            }

            protected abstract void DoUpdate(int elapsed, int totalTime);
        }
        
        private class FloatAnimatorTask : BaseAnimatorTask
        {
            private readonly float m_vector;
            private readonly Action<float> m_tickCallback;

            public FloatAnimatorTask(AnimationKind kind, float value, int time, Action<float> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_vector = value / time;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                m_tickCallback(m_vector * elapsed);
            }
        }

        private class Vector2AnimatorTask : BaseAnimatorTask
        {
            private readonly Vector2 m_vector;
            private readonly Action<Vector2> m_tickCallback;

            public Vector2AnimatorTask(AnimationKind kind, Vector2 value, int time, Action<Vector2> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_vector = value / time;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                m_tickCallback(m_vector * elapsed);
            }
        }
        
        private class Vector3AnimatorTask : BaseAnimatorTask
        {
            private readonly Vector3 m_vector;
            private readonly Action<Vector3> m_tickCallback;

            public Vector3AnimatorTask(AnimationKind kind, Vector3 value, int time, Action<Vector3> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_vector = value / time;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                m_tickCallback(m_vector * elapsed);
            }
        }
        
        private class IntAnimatorTask : BaseAnimatorTask
        {
            private readonly float m_vector;
            private readonly Action<int> m_tickCallback;

            public IntAnimatorTask(AnimationKind kind, int value, int time, Action<int> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_vector = value / (float)time;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                m_tickCallback((int)(m_vector * elapsed));
            }
        }

        private class IntPreciseAnimatorTask : BaseAnimatorTask
        {
            private readonly float m_vector;
            private readonly Action<int> m_tickCallback;

            public IntPreciseAnimatorTask(AnimationKind kind, int value, int time, Action<int> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_vector = value / (float)time;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                m_tickCallback((int)Math.Round(m_vector * totalTime));
            }
        }

        private class CustomAnimatorTask : BaseAnimatorTask
        {
            private readonly object m_param;
            private readonly Action<int, object> m_tickCallback;

            public CustomAnimatorTask(AnimationKind kind, object param, int time, Action<int, object> tickCallback, Action callback)
                : base(kind, time, callback)
            {
                m_param = param;
                m_tickCallback = tickCallback;
            }

            protected override void DoUpdate(int elapsed, int totalTime)
            {
                if (m_tickCallback != null)
                    m_tickCallback(elapsed, m_param);
            }
        }

        private readonly LinkedList<BaseAnimatorTask> m_tasks = new LinkedList<BaseAnimatorTask>();
        private long m_lastUpdate;

        public Animator()
        {
            m_lastUpdate = WindowController.Instance.GetTime();
        }

        public void Reset()
        {
            m_lastUpdate = WindowController.Instance.GetTime();
            m_tasks.Clear();
        }

        public void Update()
        {
            int elapsed = (int)(WindowController.Instance.GetTime() - m_lastUpdate);
            if (elapsed > 0)
            {

                LinkedListNode<BaseAnimatorTask> node = m_tasks.First;
                while (node != null)
                {
                    LinkedListNode<BaseAnimatorTask> next = node.Next;
                    if (node.Value.Update(elapsed))
                    {
                        m_tasks.Remove(node);
                        node.Value.Complete();   
                    }
                    node = next;
                }

                m_lastUpdate = WindowController.Instance.GetTime();
            }
        }

        public void RemoveAnimation(AnimationKind kind)
        {
            LinkedListNode<BaseAnimatorTask> node = m_tasks.First;
            while (node != null)
            {
                LinkedListNode<BaseAnimatorTask> next = node.Next;
                if (node.Value.Kind == kind)
                    m_tasks.Remove(node);
                node = next;
            }
        }
        
        public void StartAnimation(AnimationKind kind, float value, int time, Action<float> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);
            m_tasks.AddLast(new FloatAnimatorTask(kind, value, time, tick, callback));
        }

        public void StartAnimation(AnimationKind kind, int value, int time, Action<int> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);
            m_tasks.AddLast(new IntAnimatorTask(kind, value, time, tick, callback));
        }

        public void StartAnimation(AnimationKind kind, Vector2 value, int time, Action<Vector2> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);
            m_tasks.AddLast(new Vector2AnimatorTask(kind, value, time, tick, callback));
        }
        
        public void StartAnimation(AnimationKind kind, Vector3 value, int time, Action<Vector3> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);
            m_tasks.AddLast(new Vector3AnimatorTask(kind, value, time, tick, callback));
        }
        
        public void StartPreciseAnimation(AnimationKind kind, int value, int time, Action<int> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);
            m_tasks.AddLast(new IntPreciseAnimatorTask(kind, value, time, tick, callback));
        }

        public void StartCustomAnimation(AnimationKind kind, object param, int time, Action<int, object> tick, Action callback)
        {
            if (kind != AnimationKind.None)
                RemoveAnimation(kind);

            m_tasks.AddLast(new CustomAnimatorTask(kind, param, time, tick, callback));
        }
    }
}

