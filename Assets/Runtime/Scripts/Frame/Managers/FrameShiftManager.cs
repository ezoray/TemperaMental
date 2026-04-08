using TemperaMental.Applications.Config;
using TemperaMental.Frames;
using UnityEngine;
using UnityEngine.Events;

public class FrameShiftManager : MonoBehaviour
{
    int bpm;
    bool isLatched;
    float nextEventTime;
    float repeatRate;
    ShiftDirectionFlags directionFlags;

    [SerializeField] UnityEvent<ShiftDirectionFlags> onFrameShift;
    [SerializeField] UnityEvent<bool> onLatchStateChanged;
    [SerializeField] UnityEvent<int, bool> onShiftButtonLatchChanged;


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
            onFrameShift?.Invoke(directionFlags);
            nextEventTime = Time.time + repeatRate;
        }
    }

    public void OnClickLatchToggle()
    {
        isLatched = !isLatched;
        if (!isLatched)
            directionFlags = 0;

        onLatchStateChanged?.Invoke(isLatched);
    }

    public void OnClickShiftFrame(int direction)
    {
        // use flags to allows two directions at once, diagonal shifting
        ShiftDirectionFlags directionFlag = (ShiftDirectionFlags)(1 << direction);

        // if not latched just send click on as normal
        if (!isLatched)
        {
            onFrameShift?.Invoke(directionFlag);
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