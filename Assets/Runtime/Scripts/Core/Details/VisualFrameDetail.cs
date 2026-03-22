using System.Collections.Generic;

namespace TemperaMental.Core
{
    public struct VisualFrameDetail
    {
        public int FrameNumber;
        public int FrameTotal;
        public List<VisualEmitterDetail> EmitterDetails;

        public VisualFrameDetail(int frameNumber, int frameTotal, List<VisualEmitterDetail> emitterDetails)
        {
            FrameNumber = frameNumber;
            FrameTotal = frameTotal;
            EmitterDetails = emitterDetails;
        }
    }
}
