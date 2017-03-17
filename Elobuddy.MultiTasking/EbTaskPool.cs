using System;
using System.Collections;
using System.Collections.Generic;
using EloBuddy;

namespace Elobuddy.MultiTasking
{
    public static class EbTaskPool
    {
        private static List<EbTask> Pool = new List<EbTask>();

        public static void Init()
        {
            FPSManager.Init();
            Game.OnUpdate += OnUpdate;
        }

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

        public static EbTask Add(EbTask t)
        {
            Pool.Add(t);
            return t;
        }

        public static EbTask Start(this EbTask t)
        {
            Pool.Add(t);
            return t;
        }

        public static bool IsRunning(this EbTask t) => Pool.Contains(t);
        public static bool IsPaused(this EbTask t) => t.HasToWait || FPSManager.HasFpsDrop(t.FpsDropDelayTime);

        /// <summary>
        /// Returns the origin task
        /// </summary>
        public static EbTask ContinueWith(this EbTask t, EbTask task)
        {
            if (!Pool.Contains(t))
                throw new Exception("ContinueWith call of a task that isn't contained in the task pool");

            Pool.Find(x => x == t).AddTask(task);
            return t;
        }

        /// <summary>
        /// Returns the origin task
        /// </summary>
        public static EbTask ContinueWith(this EbTask t, IEnumerator task, bool DelayOnFpsDrop = true)
        {
            if (!Pool.Contains(t))
                throw new Exception("ContinueWith call of a task that isn't contained in the task pool");

            Pool.Find(x => x == t).AddTask(new EbTask(task, DelayOnFpsDrop));
            return t;
        }

        public static EbTask ContinueWith(this EbTask t, Action callBackFunc)
        {
            if (!Pool.Contains(t))
                throw new Exception("ContinueWith call of a task that isn't contained in the task pool");

            Pool.Find(x => x == t).AddCallback(callBackFunc);
            return t;
        }

        private static void OnUpdate(EventArgs args)
        {
            List<EbTask> finishedTasks = new List<EbTask>();
            List<EbTask> TasksToAdd = new List<EbTask>();

            foreach (var task in Pool)
            {
                ManageTask(finishedTasks, TasksToAdd, task);
            }

            Pool.RemoveAll(x => finishedTasks.Contains(x));
            Pool.AddRange(TasksToAdd);
        }

        private static Tuple<EbTask, bool> GetDeepestTask(EbTask t, bool IsAwaiter = false)
        {
            if (t.HasAwaiter)
                return GetDeepestTask(t.Awaiter, true);


            return new Tuple<EbTask, bool>(t, IsAwaiter);
        }

        private static void SetDeepAwaiterNull(EbTask mainTask, EbTask awaiter)
        {
            if (mainTask.Awaiter == awaiter)
            {
                mainTask.Awaiter = null;
                return;
            }

            SetDeepAwaiterNull(mainTask.Awaiter, awaiter);
        }

        private static void ManageTask(List<EbTask> finishedTasks, List<EbTask> TasksToAdd, EbTask task)
        {
            var dT = GetDeepestTask(task);
            var deepTask = dT.Item1;
            bool wasAwaiter = dT.Item2;

            if (deepTask.HasToWait)
                return;

            if (deepTask.StopOnDrop && FPSManager.HasFpsDrop(task.FpsDropDelayTime))
                return;

            IEnumerator enumeratorContainer = deepTask.FuncEnumerator;
            bool couldMoveOn = enumeratorContainer.MoveNext();

            if (couldMoveOn)
            {
                object returnVal = enumeratorContainer.Current;
                if (returnVal != null)
                {
                    if (returnVal is EbSleep)
                    {
                        EbSleep sleepInstance = (EbSleep)returnVal;
                        deepTask.SetWait(sleepInstance.Milliseconds);
                        return;
                    }

                    if (returnVal is EbAwait)
                    {
                        var ebAwait = (EbAwait)returnVal;
                        var awaitTask = ebAwait.Task;
                        deepTask.Awaiter = awaitTask;
                        return;
                    }

                    deepTask.ReturnValue = returnVal;
                }
            }

            if (!couldMoveOn)
            {
                deepTask.OnTaskCompleted();

                foreach (var continueFunc in deepTask.ContinueFuncs)
                {
                    continueFunc.Invoke();
                }

                foreach (var continueTask in deepTask.ContinueTasks)
                {
                    TasksToAdd.Add(continueTask);
                }

                if (!wasAwaiter)
                    finishedTasks.Add(task);
                else
                    SetDeepAwaiterNull(task, deepTask);
            }
        }
    }
}
