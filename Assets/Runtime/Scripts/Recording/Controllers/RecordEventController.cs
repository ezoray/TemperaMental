using TemperaMental.Core;
using TemperaMental.Midi.Input;
using UnityEngine;

namespace TemperaMental.Recording
{
    public class RecordEventController : MonoBehaviour
    {
        [SerializeField] MidiRecorder recorder;

        // arms, disarms or stops a recording from the Tempera, or toggles recording transform results (Source setting)
        public void OnClickToggleRecord() => recorder.ToggleRecord();

        // wired after the device record and Follow Tempera listeners
        public void ActionOnTemperaControlEvent(TemperaControlEvent controlEvent) => recorder.ActionOnTemperaControlEvent(controlEvent);

        // Record settings
        public void ActionOnRecordSourceChanged(RecordSource source) => recorder.SetSource(source);

        public void ActionOnRecordTriggerChanged(RecordTrigger trigger) => recorder.SetTrigger(trigger);

        public void ActionOnRecordModeChanged(RecordMode mode) => recorder.SetMode(mode);

        public void ActionOnRecordStartStateChanged(RecordStartState startState) => recorder.SetStartState(startState);

        public void ActionOnRecordLengthChanged(int frames) => recorder.SetLength(frames);
    }
}
