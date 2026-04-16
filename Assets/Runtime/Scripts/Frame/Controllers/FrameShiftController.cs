using TemperaMental.Applications.Config;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Frames
{
    public class FrameShiftController: MonoBehaviour
    {
        bool isLatched;
        bool doWrap;
        ShiftDirectionFlags directionFlags;

        int bpm;
        float nextEventTime;
        float repeatRate;

        [SerializeField] UnityEvent<bool> onLatchStateChanged;
        [SerializeField] UnityEvent<bool> onWrapStateChanged;
        [SerializeField] UnityEvent<int, bool> onShiftButtonLatchChanged;
        [SerializeField] UnityEvent<ShiftDirectionFlags, bool> onShiftFrame;


        private void Awake()
        {
            bpm = ConfigRegistry.Midi.DefaultBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + repeatRate;
        }

        void Update()
        {
            if (!isLatched || directionFlags == 0) return;

            if (Time.time >= nextEventTime)
            {
                onShiftFrame?.Invoke(directionFlags, doWrap);
                nextEventTime = Time.time + repeatRate;
            }
        }

        public void ToggleWrapping()
        {
            doWrap = !doWrap;

            onWrapStateChanged?.Invoke(doWrap);
        }

        public void ToggleLatch()
        {
            isLatched = !isLatched;
            if (!isLatched)
                directionFlags = 0;

            onLatchStateChanged?.Invoke(isLatched);
        }

        public void ShiftFrame(int direction)
        {
            // use flags to allow two directions at once, diagonal shifting
            ShiftDirectionFlags directionFlag = (ShiftDirectionFlags)(1 << direction);

            // if not latched just send click on as normal
            if (!isLatched)
            {
                onShiftFrame?.Invoke(directionFlag, doWrap);
                return;
            }

            // latched enabled and direction already latched, clear it
            if (directionFlags.HasFlag(directionFlag))
            {
                directionFlags &= ~directionFlag;

                onShiftButtonLatchChanged?.Invoke(direction, false);
                return;
            }

            // otherwise direction not already latched, set it and clear opposing direction
            directionFlags &= ~(ShiftDirectionFlags)(1 << (direction ^ 1));
            onShiftButtonLatchChanged?.Invoke(direction ^ 1, false);

            directionFlags |= directionFlag;
            onShiftButtonLatchChanged?.Invoke(direction, true);

            nextEventTime = Time.time;
        }

        public void ActionOnBpmChanged(int newBpm)
        {
            bpm = newBpm;
            repeatRate = 60f / bpm;
            nextEventTime = Time.time + (repeatRate / 4);
        }
    }
}