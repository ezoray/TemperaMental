using TemperaMental.Core;
using TemperaMental.Midi.Input;
using UnityEngine;

namespace TemperaMental.Recording
{
    public class RecordEventController : MonoBehaviour
    {
        [SerializeField] MidiRecorder recorder;

        // the Record page's Record: arms, disarms or stops a recording from the Tempera
        public void OnClickRecordTempera() => recorder.ToggleRecordTempera();

        // the Transform page's Rec results: toggles recording transform results as new frames
        public void OnClickRecordTransforms() => recorder.ToggleRecordTransforms();

        // wired after the device record and Follow Tempera listeners
        public void ActionOnTemperaControlEvent(TemperaControlEvent controlEvent) => recorder.ActionOnTemperaControlEvent(controlEvent);

        // notes played on the Tempera (any channel but the Control channel)
        public void ActionOnPlayedNote(TemperaNoteEvent noteEvent) => recorder.ActionOnPlayedNote(noteEvent);

        // Record settings
        public void ActionOnRecordTriggerChanged(RecordTrigger trigger) => recorder.SetTrigger(trigger);

        public void ActionOnRecordModeChanged(RecordMode mode) => recorder.SetMode(mode);

        public void ActionOnRecordStartStateChanged(RecordStartState startState) => recorder.SetStartState(startState);

        public void ActionOnRecordLengthChanged(int frames) => recorder.SetLength(frames);
    }
}
