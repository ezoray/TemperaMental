using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Settings.Core
{
    public class SettingsUIManager : MonoBehaviour
    {
        [SerializeField] GameObject[] views;

        int viewIndex = -1;

        [SerializeField] UnityEvent<GameObject> OnViewChanged;
        [SerializeField] UnityEvent onSettingsViewClosed;


        public void OnClickSettings()
        {
            viewIndex++;

            if (viewIndex >= views.Length)
            {
                viewIndex = -1;
                onSettingsViewClosed?.Invoke();

                return;
            }

            OnViewChanged?.Invoke(views[viewIndex]);
        }
    }
}
