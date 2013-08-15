using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimationModels
{
    [Serializable]
    public class EndFrame
    {
        private TimeSpan startTime;

        public EndFrame() 
            : this(new TimeSpan())
        {
        }

        public EndFrame(TimeSpan StartTime)
        {
            this.StartTime = StartTime;
        }

        public TimeSpan StartTime
        {
            get
            {
                return this.startTime;
            }
            set
            {
                if (value != this.startTime)
                {
                    this.startTime = value;
                }
            }
        }
    }
}
