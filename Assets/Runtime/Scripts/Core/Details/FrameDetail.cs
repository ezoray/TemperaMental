using TemperaMental.Utils;

namespace TemperaMental.Core
{
    public struct FrameDetail
    {
        public readonly int FrameNumber;
        public readonly int FrameTotal;
        public readonly ulong[] EmitterGroups;
        public readonly int EmitterCount;

        public FrameDetail(int frameNumber, int frameTotal, ulong[] emitterGroups)
        {
            FrameNumber = frameNumber;
            FrameTotal = frameTotal;
            EmitterGroups = emitterGroups;

            EmitterCount = EmitterUtils.GetEmitterCount(emitterGroups);
        }
    }
}
