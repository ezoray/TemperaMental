using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Diagnostics;
using System.Threading;

namespace TemperaMental
{
    public class SpinWaitTickGenerator : TickGenerator
    {
        private Thread _thread;
        private volatile bool _isRunning;

        protected override void Start(TimeSpan interval)
        {
            long intervalTicks = (long)(interval.TotalSeconds * Stopwatch.Frequency);

            _isRunning = true;
            _thread = new Thread(() =>
            {
                long next = Stopwatch.GetTimestamp();

                while (_isRunning)
                {
                    next += intervalTicks;
                    while (Stopwatch.GetTimestamp() < next)
                    {
                        if (!_isRunning) return;
                    }
                    if (_isRunning)
                        GenerateTick();
                }
            });

            _thread.IsBackground = true;
            _thread.Priority = ThreadPriority.Highest;
            _thread.Start();
        }

        protected override void Stop()
        {
            _isRunning = false;
        }
    }
}
