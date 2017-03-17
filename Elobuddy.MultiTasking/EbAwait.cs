﻿using System.Collections;

namespace Elobuddy.MultiTasking
{
    public class EbAwait
    {
        internal EbTask Task;
        public EbAwait(IEnumerator Task, bool PauseOnFpsDrop = true)
        {
            SetTask(new EbTask(Task, PauseOnFpsDrop));
        }

        public EbAwait(EbTask Task)
        {
            SetTask(Task);
        }

        private void SetTask(EbTask t) => Task = t;
    }
}
