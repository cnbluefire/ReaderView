using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderView.Common.Helpers
{
    public class EventTimeFliter
    {
        private DateTime _lastTime;

        public EventTimeFliter(double seconds)
        {
            Interval = TimeSpan.FromSeconds(seconds);
        }

        public EventTimeFliter(TimeSpan interval)
        {
            Interval = interval;
        }

        public EventTimeFliter()
        {
            Interval = TimeSpan.FromSeconds(0.1d);
        }

        public TimeSpan Interval { get; set; }

        public bool IsEnable
        {
            get
            {
                if(DateTime.Now - _lastTime > Interval)
                {
                    _lastTime = DateTime.Now;
                    return true;
                }
                return false;
            }
        }

        public void Reset()
        {
            _lastTime = DateTime.Now;
        }
    }
}
