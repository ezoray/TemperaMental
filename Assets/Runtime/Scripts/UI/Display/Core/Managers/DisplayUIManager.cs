using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Display.Core
{
    public class DisplayUIManager : MonoBehaviour
    {
        [SerializeField] GameObject mainView;
        GameObject currentView;

        private void Awake()
        {
            currentView = mainView;
        }

        public void ActionOnViewClosed()
        {
            SetMainViewActive();
        }    

        public void ActionOnViewChanged(GameObject newView)
        {
            currentView.SetActive(false);

            currentView = newView;
            currentView.SetActive(true);
        }

        public void ActionOnRecordingStateChanged(bool isRecording)
        {
            SetMainViewActive();
        }

        public void ActionOnPlaybackStateChanged(PlaybackState playbackState)
        {
            SetMainViewActive();
        }

        private void SetMainViewActive()
        {
            if (currentView != mainView)
            {
                currentView.SetActive(false);

                currentView = mainView;
                currentView.SetActive(true);
            }
        }
    }
}
