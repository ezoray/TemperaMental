using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Display.Core
{
    // added to settings views to pick up when they close so that settings can be set on close rather than instant chanage
    public class ViewClosedNotifier : MonoBehaviour
    {
        [SerializeField] UnityEvent onViewClosed;

        private void OnDisable()
        {
            onViewClosed?.Invoke();
        }
    }
}
