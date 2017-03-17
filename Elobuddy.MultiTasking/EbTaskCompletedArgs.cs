using System;

namespace Elobuddy.MultiTasking
{
    public class EbTaskCompletedArgs : EventArgs
    {
        public EbTaskCompletedArgs(object returnValue, bool hasReturnValue)
        {
            ReturnValue = returnValue;
            HasReturnValue = hasReturnValue;
        }

        public object ReturnValue { get; }
        public bool HasReturnValue { get; }
    }
}
