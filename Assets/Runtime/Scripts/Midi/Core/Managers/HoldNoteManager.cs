using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using TemperaMental.Midi.Output;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Core
{
    // Hold: keeps one note on (middle C on the app's MIDI channel), so the Tempera sounds whatever emitters are placed
    // without a note being played on it: drawing, transforms, and playback of frames without notes. The Tempera needs a
    // voice playing to make sound. Not remembered between runs; the output scheduler releases it when the app quits.
    public class HoldNoteManager : MonoBehaviour
    {
        [SerializeField] MidiOutputScheduler outputScheduler;

        [SerializeField] UnityEvent<bool> onHoldChanged = new UnityEvent<bool>();

        int note;
        int velocity;
        int channel;
        bool isHolding;

        private void Awake()
        {
            note = Mathf.Clamp(ConfigRegistry.Midi.HoldNote, 0, 127);
            velocity = Mathf.Clamp(ConfigRegistry.Midi.HoldVelocity, 1, 127);
            channel = ConfigRegistry.Midi.DefaultMidiChannel - 1;
        }

        private void Start()
        {
            onHoldChanged?.Invoke(isHolding);
        }

        public bool IsHolding => isHolding;

        public void ToggleHold() => SetHold(!isHolding);

        public void SetHold(bool hold)
        {
            if (hold == isHolding) return;

            isHolding = hold;

            if (isHolding) outputScheduler.EnqueueNoteOn(channel, note, velocity);
            else outputScheduler.EnqueueNoteOff(channel, note);

            LogMan.LogTemp(isHolding ? $"Holding note {Frames.FrameEvent.NoteName(note)}" : "Hold off");
            onHoldChanged?.Invoke(isHolding);
        }

        // 1–16: a held note moves to the new channel
        public void SetMidiChannel(int newChannel)
        {
            int zeroBased = Mathf.Clamp(newChannel - 1, 0, 15);

            if (zeroBased == channel) return;

            if (isHolding) outputScheduler.EnqueueNoteOff(channel, note);

            channel = zeroBased;

            if (isHolding) outputScheduler.EnqueueNoteOn(channel, note, velocity);
        }

        // a newly selected output device gets the held note too
        public void ActionOnPlaybackReadyStateChanged(bool isReady)
        {
            if (isReady && isHolding) outputScheduler.EnqueueNoteOn(channel, note, velocity);
        }
    }
}
