namespace TemperaMental.Core
{
    public struct FrameDetail
    {
        public int FrameNumber;
        public int FrameTotal;
        public ulong[] EmitterGroups;

        public FrameDetail(int frameNumber, int frameTotal, ulong[] emitterGroups)
        {
            FrameNumber = frameNumber;
            FrameTotal = frameTotal;
            EmitterGroups = emitterGroups;
        }
    }
}
