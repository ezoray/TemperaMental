using System.Collections;
using System.Linq;
using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TemperaMental.UI.Core
{
    public class KeyHandlerBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected InputActionReference actionReference;

        string shortcutText;
        string boundKeyName;
        Coroutine hoverCoroutine;
        float shortcutMessageDelay;

        protected virtual void Awake()
        {
            shortcutText = ConfigRegistry.UI.ShortcutText;
            shortcutMessageDelay = ConfigRegistry.UI.ShortcutMessageDelay;

            boundKeyName = string.Join(", ", actionReference.action.bindings
                .Select(binding => binding.ToDisplayString())
                .Where(keyName => !string.IsNullOrEmpty(keyName)));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hoverCoroutine = StartCoroutine(DelayedShortcutMessage());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
                hoverCoroutine = null;
            }
        }

        private IEnumerator DelayedShortcutMessage()
        {
            yield return new WaitForSeconds(shortcutMessageDelay);
            LogMan.LogTemp($"{shortcutText} {boundKeyName}");
        }

    }
}
