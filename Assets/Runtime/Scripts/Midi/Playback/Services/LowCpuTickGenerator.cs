using System;
using System.Threading;
using Melanchall.DryWetMidi.Multimedia;

namespace TemperaMental.Midi.Playbacks
{
    public class LowCpuTickGenerator : TickGenerator
    {
        private Thread thread;
        private volatile bool isRunning;

        protected override void Start(TimeSpan interval)
        {
            isRunning = true;
            thread = new Thread(() =>
            {
                while (isRunning)
                {
                    Thread.Sleep(interval);
                    if (isRunning)
                        GenerateTick();
                }
            });
            thread.IsBackground = true;
            thread.Priority = System.Threading.ThreadPriority.BelowNormal;
            thread.Start();
        }

        protected override void Stop()
        {
            isRunning = false;
            // Don't Join — just let the background thread die naturally
        }
    }
}
