using System.Collections;

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

        public object AwaitReturn;

        public void SetNull()
        {
            IsNull = true;
        }

        public bool IsNull { get; set; }

        private void SetTask(EbTask t) => Task = t;
    }
}
