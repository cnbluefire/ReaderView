using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace ReaderView.Common.Helpers
{
    public class EventWaiter
    {
        private DispatcherTimer _timer;

        public EventWaiter(double seconds) : this(TimeSpan.FromSeconds(seconds))
        {
        }

        public EventWaiter(TimeSpan interval)
        {
            _timer = new DispatcherTimer();
            _timer.Tick += _timer_Tick;
            Interval = interval;
        }

        public EventWaiter() : this(0.1)
        {
        }

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool ResetWhenWaitCall { get; set; }

        public void Wait()
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
            else
            {
                if (ResetWhenWaitCall)
                {
                    _timer.Stop();
                    _timer.Start();
                }
            }
        }


        private void _timer_Tick(object sender, object e)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            OnArrived();
        }

        public event EventHandler Arrived;
        protected void OnArrived()
        {
            Arrived?.Invoke(this, EventArgs.Empty);
        }

    }
}
