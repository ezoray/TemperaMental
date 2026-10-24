using TemperaMental.Core;
using TemperaMental.Frames;
using TemperaMental.Logs;
using TemperaMental.Midi.Input;
using TemperaMental.Settings;
using UnityEngine;

namespace TemperaMental.UI.Pages
{
    // When a page opens, the display's message line says what to do there, or what's missing first
    public class PageHintUIManager : MonoBehaviour
    {
        [SerializeField] FrameManager frameManager;
        [Tooltip("The In device's router: recording from the Tempera needs it")]
        [SerializeField] MidiInputRouter inputRouter;
        [SerializeField] ClockSettingsManager clockSettings;

        public void ActionOnPageChanged(AppPage page)
        {
            LogMan.Log(GetHint(page));
        }

        private string GetHint(AppPage page)
        {
            switch (page)
            {
                case AppPage.Compose:
                    return "Draw, then New or Duplicate for the next frame";

                case AppPage.Play:
                    if (clockSettings != null && clockSettings.ClockMode == ClockMode.Send && clockSettings.IsClockToNone)
                        return "Clock To is None, so no clock is sent";
                    return "Press Play. Hold keeps a note on to hear it";

                case AppPage.Transform:
                    if (IsFrameEmpty()) return "Draw some emitters first, then transform them";
                    return "Pick a transform, then press a direction";

                case AppPage.Record:
                    if (inputRouter != null && !inputRouter.IsListening) return "Choose the Tempera as In to record";
                    return "Press Record, then touch the Tempera";

                default:
                    return "Set these once to match your Tempera";
            }
        }

        private bool IsFrameEmpty()
        {
            if (frameManager == null) return false;

            var frame = frameManager.PublishedCurrentFrame;
            if (frame == null) return true;

            foreach (ulong group in frame.StartGroups)
            {
                if (group != 0) return false;
            }
            return true;
        }
    }
}
