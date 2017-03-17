using System;
using System.Collections;
using System.Collections.Generic;
using EloBuddy;

namespace Elobuddy.MultiTasking
{
    static class FPSManager
    {
        private static List<float> FpsList = new List<float>(100);

        public static void Init()
        {
            set = Environment.TickCount;
            Game.OnUpdate += GameOnOnUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private static int calls, set;
        private static float FPS;
        public static float FPS_DROP_PERCENTAGE { get; internal set; } = 20;
        private static void DrawingOnOnDraw(EventArgs args)
        {
            calls++;
        }

        private static int LastFpsDrop;
        public static bool HasFpsDrop => Environment.TickCount - LastFpsDrop < 1000;

        private static void GameOnOnUpdate(EventArgs args)
        {
            GetFPS();

            if (FpsList.Count > 1)
            {
                var previous = FpsList[FpsList.Count - 2];
                var current = FpsList[FpsList.Count - 1];
                var m = current - previous;

                if (m < -FPS*FPS_DROP_PERCENTAGE/100)
                {
                    LastFpsDrop = Environment.TickCount;
                }
            }
        }

        private static void GetFPS()
        {
            if (Environment.TickCount - set < 1000)
            {
                return;
            }
            set = Environment.TickCount;

            FPS = calls;
            calls = 0;

            if (FpsList.Count >= 100)
                FpsList.Clear();

            FpsList.Add(FPS);
        }
    }

    public class EbSleep
    {
        public int Milliseconds { get; }
        public EbSleep(int milliseconds)
        {
            Milliseconds = milliseconds;
        }
    }

    public class EbTask
    {
        public IEnumerator FuncEnumerator { get; }
        public List<Action> ContinueFuncs = new List<Action>();
        internal object ReturnValue { get; set; }
        public bool StopOnDrop { get; }

        private int LastWaitSet;
        private int LastWaitTime;

        public EbTask(IEnumerator funcEnumerator, bool PauseOnFpsDrop = true)
        {
            StopOnDrop = PauseOnFpsDrop;
            FuncEnumerator = funcEnumerator;
        }

        public bool HasToWait => Environment.TickCount - LastWaitSet < LastWaitTime;

        public void SetWait(int time)
        {
            LastWaitSet = Environment.TickCount;
            LastWaitTime = time;
        }

        public void AddCallback(Action f)
        {
            ContinueFuncs.Add(f);
        }

        public delegate void CompletedH(object ReturnValue);
        public event CompletedH TaskCompleted;

        internal void OnTaskCompleted()
        {
            TaskCompleted?.Invoke(ReturnValue);
        }
    }

    public static class EbTaskPool
    {
        private static List<EbTask> Pool = new List<EbTask>();

        /// <summary>
        /// Pauses All Tasks If The FPS Decrease Over X Percent.
        /// </summary>
        /// <param name="PerCent">0-100</param>
        public static void SetFpsThreshold(float PerCent)
        {
            if (PerCent < 1 || PerCent > 100)
                throw new Exception("FPS Threshold out of range [0;100]");

            FPSManager.FPS_DROP_PERCENTAGE = PerCent;
        }

        public static void Add(EbTask t)
        {
            Pool.Add(t);
        }

        public static EbTask Start(this EbTask t)
        {
            Pool.Add(t);
            return t;
        }

        public static EbTask ContinueWith(this EbTask t, Action callBackFunc)
        {
            if (!Pool.Contains(t))
                throw new Exception("ContinueWith call of a task that isn't contained in the task pool");

            Pool.Find(x => x == t).AddCallback(callBackFunc);
            return t;
        }

        public static void Init()
        {
            FPSManager.Init();
            /*Loading.OnLoadingComplete += argss =>*/ Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            List<EbTask> finishedTasks = new List<EbTask>();
            foreach (var task in Pool)
            {
                if (task.HasToWait)
                    continue;

                IEnumerator enumeratorContainer = task.FuncEnumerator;
                EbSleep sleepInstance = null;

                if (task.StopOnDrop && FPSManager.HasFpsDrop)
                    continue;

                bool couldMoveOn = enumeratorContainer.MoveNext();

                if (couldMoveOn)
                {
                    object returnVal = enumeratorContainer.Current;
                    if (returnVal is EbSleep)
                    {
                        var sleep = (EbSleep) returnVal;
                        sleepInstance = sleep;
                    }
                    else
                    {
                        task.ReturnValue = returnVal;
                    }
                }

                if (sleepInstance != null)
                {
                    task.SetWait(sleepInstance.Milliseconds);
                }
                else if (!couldMoveOn)
                {
                    task.OnTaskCompleted();

                    foreach (var continueFunc in task.ContinueFuncs)
                    {
                        continueFunc.Invoke();
                    }

                    finishedTasks.Add(task);
                }
            }

            Pool.RemoveAll(x => finishedTasks.Contains(x));
        }
    }
}
