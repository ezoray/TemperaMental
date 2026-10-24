using System;
using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Modes
{
    // A set of controls shown in some modes and hidden in the others. A hidden group stays active (alpha 0, no
    // raycasts), so its keyboard shortcuts keep working in every mode: a key handler stops listening when its object is
    // disabled, and switches its input action off for every other handler too.
    // A group can sit somewhere else in a given mode, e.g. the frame buttons move down in Edit mode to make room for the
    // frame edit buttons. Other modes use the position it has in the scene.
    [RequireComponent(typeof(CanvasGroup))]
    public class ModeGroup : MonoBehaviour
    {
        [Serializable]
        struct ModePosition
        {
            public AppMode mode;
            public Vector2 anchoredPosition;
        }

        [SerializeField] AppModes modes;
        [SerializeField] ModePosition[] positions = new ModePosition[0];

        CanvasGroup canvasGroup;
        RectTransform rectTransform;
        Vector2 scenePosition;

        // ModeUIManager can apply the first mode before this group's own Awake
        private void Initialise()
        {
            if (canvasGroup != null) return;

            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = (RectTransform)transform;
            scenePosition = rectTransform.anchoredPosition;
        }

        public void Apply(AppMode mode)
        {
            Initialise();

            bool isShown = (modes & (AppModes)(1 << (int)mode)) != 0;

            // interactable is left alone: UIStateManager gates some of these groups by playback state
            canvasGroup.alpha = isShown ? 1f : 0f;
            canvasGroup.blocksRaycasts = isShown;

            if (!isShown) return;

            Vector2 position = scenePosition;

            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].mode == mode) position = positions[i].anchoredPosition;
            }

            rectTransform.anchoredPosition = position;
        }
    }
}
