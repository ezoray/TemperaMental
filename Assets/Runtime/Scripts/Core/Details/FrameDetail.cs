using System.Collections.Generic;
using TemperaMental.Frames;

namespace TemperaMental.Core
{
    public struct FrameDetail
    {
        public int FrameNumber;
        public int FrameTotal;
        public List<EmitterDetail> EmitterDetails;

        public FrameDetail(int frameNumber, int frameTotal, List<EmitterDetail> emitterDetails)
        {
            FrameNumber = frameNumber;
            FrameTotal = frameTotal;
            EmitterDetails = emitterDetails;
        }
    }
}
