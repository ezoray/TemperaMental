using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Settings.Core
{
    public class SettingsUIManager : MonoBehaviour
    {
        [SerializeField] GameObject[] views;
        [SerializeField] UnityEvent<GameObject> onViewChanged;
        [SerializeField] UnityEvent onSettingsClosed;

        int viewIndex = -1;

        public void OnClickSettings()
        {
            if (viewIndex >= 0)
            {
                CloseCurrentView();
                viewIndex = -1;
                onSettingsClosed?.Invoke();
                return;
            }
            viewIndex = 0;
            OpenView();
        }

        public void OnClickNext()
        {
            CloseCurrentView();
            viewIndex = (viewIndex + 1) % views.Length;
            OpenView();
        }

        public void OnClickPrev()
        {
            CloseCurrentView();
            viewIndex = (viewIndex - 1 + views.Length) % views.Length;
            OpenView();
        }

        private void OpenView()
        {
            onViewChanged?.Invoke(views[viewIndex]);
        }

        private void CloseCurrentView()
        {
            if (viewIndex >= 0 && viewIndex < views.Length)
                views[viewIndex].SetActive(false);
        }
    }
}
