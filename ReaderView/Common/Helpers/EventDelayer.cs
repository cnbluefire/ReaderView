using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace ReaderView.Common.Helpers
{
    public class EventDelayer
    {
        private DispatcherTimer _timer;

        public EventDelayer(double seconds) : this(TimeSpan.FromSeconds(seconds))
        {
        }

        public EventDelayer(TimeSpan interval)
        {
            _timer = new DispatcherTimer();
            _timer.Tick += _timer_Tick;
            Interval = interval;
        }

        public EventDelayer() : this(0.1)
        {
        }

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool ResetWhenDelayed { get; set; }

        public void Delay()
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
            else
            {
                if (ResetWhenDelayed)
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
