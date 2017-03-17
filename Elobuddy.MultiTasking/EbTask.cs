using System;
using System.Collections;
using System.Collections.Generic;

namespace Elobuddy.MultiTasking
{
    public class EbTask
    {
        public string Name { get; set; }

        public delegate void CompletedH(EbTaskCompletedArgs args);
        public event CompletedH TaskCompleted;

        /// <summary>
        /// Delay Time Of The Task In Milliseconds If Fps Drop
        /// </summary>
        public float FpsDropDelayTime { get; set; } = 1000;
        internal bool HasToWait => Environment.TickCount - LastWaitSet < LastWaitTime;
        internal IEnumerator FuncEnumerator { get; }
        internal List<Action> ContinueFuncs = new List<Action>();
        internal List<EbTask> ContinueTasks = new List<EbTask>();
        internal EbTask Awaiter;

        public bool HasAwaiter => Awaiter != null;

        private object _returnValue;
        internal object ReturnValue
        {
            get { return _returnValue; }
            set
            {
                _returnValue = value;
                HasReturnValue = true;
            }
        }

        private bool HasReturnValue;
        internal bool StopOnDrop { get; }

        private int LastWaitSet;
        private int LastWaitTime;


        public EbTask(IEnumerator funcEnumerator, bool PauseOnFpsDrop = true)
        {
            StopOnDrop = PauseOnFpsDrop;
            FuncEnumerator = funcEnumerator;
        }

        internal void SetWait(int time)
        {
            LastWaitSet = Environment.TickCount;
            LastWaitTime = time;
        }

        internal void AddCallback(Action f)
        {
            ContinueFuncs.Add(f);
        }

        internal void OnTaskCompleted()
        {
            TaskCompleted?.Invoke(new EbTaskCompletedArgs(ReturnValue, HasReturnValue));
        }

        internal void AddTask(EbTask task)
        {
            ContinueTasks.Add(task);
        }
    }
}
