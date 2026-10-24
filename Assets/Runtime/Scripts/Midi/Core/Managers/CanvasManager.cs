using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using TemperaMental.Midi.Output;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Midi.Core
{
    // The Tempera canvas a sequence goes with: a Program Change 0–127 (shown as 1–128), or none. The Tempera loads
    // the canvas at that alphabetical position in the folder of the canvas it has loaded. Saved in the file and sent
    // when the file is loaded; stepping it sends it once it has stopped changing for a moment, so scrolling past
    // canvases doesn't load each one.
    public class CanvasManager : MonoBehaviour
    {
        public const int None = -1;

        [SerializeField] MidiOutputScheduler outputScheduler;

        // program 0–127, or None
        [SerializeField] UnityEvent<int> onCanvasChanged = new UnityEvent<int>();

        float sendDelay;
        int program = None;
        float sendTime = -1;

        private void Awake()
        {
            sendDelay = ConfigRegistry.Midi.CanvasSendDelay;
        }

        private void Start()
        {
            onCanvasChanged?.Invoke(program);
        }

        private void Update()
        {
            if (sendTime >= 0 && Time.unscaledTime >= sendTime)
            {
                sendTime = -1;
                Send();
            }
        }

        public int Program => program;

        // -1 or +1: none, then canvas 1 to 128
        public void StepCanvas(int directionValue)
        {
            int stepped = Mathf.Clamp(program + directionValue, None, 127);

            if (stepped == program) return;

            program = stepped;
            onCanvasChanged?.Invoke(program);

            sendTime = Time.unscaledTime + sendDelay;
        }

        // from a loaded file: sent straight away
        public void SetCanvasFromFile(int fileProgram)
        {
            program = fileProgram >= 0 && fileProgram <= 127 ? fileProgram : None;
            onCanvasChanged?.Invoke(program);

            sendTime = -1;
            Send();
        }

        private void Send()
        {
            if (program == None) return;

            outputScheduler.EnqueueProgramChange(program);
            LogMan.LogTemp($"Canvas {program + 1} sent");
        }
    }
}
