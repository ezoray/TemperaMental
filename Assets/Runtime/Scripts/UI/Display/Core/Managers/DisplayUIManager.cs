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
            currentView.SetActive(false);

            currentView = mainView;
            currentView.SetActive(true);
        }    

        public void ActionOnViewChanged(GameObject newView)
        {
            currentView.SetActive(false);

            currentView = newView;
            currentView.SetActive(true);
        }
    }
}
