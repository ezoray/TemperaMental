using TemperaMental.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Display.Core
{
    public class DisplayUIManager : MonoBehaviour
    {
        [SerializeField] GameObject[] displayViews;

        int currentViewIndex;

        [SerializeField] UnityEvent<DisplayViewType> onSettingsViewClosed;


        public void OnClickSettings()
        {
            int previousViewIndex = currentViewIndex;
            displayViews[previousViewIndex].SetActive(false);

            currentViewIndex = (currentViewIndex + 1) % displayViews.Length;

            displayViews[currentViewIndex].SetActive(true);

            if (previousViewIndex > 0 && currentViewIndex == 0)
                onSettingsViewClosed?.Invoke((DisplayViewType)previousViewIndex);
        }
    }
}
