using System;
using TemperaMental.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace TemperaMental.UI.Pages
{
    // A set of controls shown on some pages and hidden on the others. A hidden group stays active (alpha 0, no
    // raycasts), so its keyboard shortcuts keep working on every page: a key handler stops listening when its object is
    // disabled, and switches its input action off for every other handler too.
    // A group can sit somewhere else on a given page, e.g. the frame buttons move down on the Compose page to make room
    // for the frame edit buttons. Other pages use the position it has in the scene.
    [RequireComponent(typeof(CanvasGroup))]
    public class PageGroup : MonoBehaviour
    {
        [Serializable]
        struct PagePosition
        {
            [FormerlySerializedAs("mode")] public AppPage page;
            public Vector2 anchoredPosition;
        }

        [FormerlySerializedAs("modes")]
        [SerializeField] AppPages pages;
        [SerializeField] PagePosition[] positions = new PagePosition[0];

        CanvasGroup canvasGroup;
        RectTransform rectTransform;
        Vector2 scenePosition;

        // PageUIManager can apply the first page before this group's own Awake
        private void Initialise()
        {
            if (canvasGroup != null) return;

            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = (RectTransform)transform;
            scenePosition = rectTransform.anchoredPosition;
        }

        public void Apply(AppPage page)
        {
            Initialise();

            bool isShown = (pages & (AppPages)(1 << (int)page)) != 0;

            // interactable is left alone: UIStateManager gates some of these groups by playback state
            canvasGroup.alpha = isShown ? 1f : 0f;
            canvasGroup.blocksRaycasts = isShown;

            if (!isShown) return;

            Vector2 position = scenePosition;

            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].page == page) position = positions[i].anchoredPosition;
            }

            rectTransform.anchoredPosition = position;
        }
    }
}
