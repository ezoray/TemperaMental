using TemperaMental.Core;
using UnityEngine;

namespace TemperaMental.UI.Pages
{
    // Renderers outside the UI shown only on some pages. The grid's tilemaps are drawn after the UI, so they'd show
    // through the Setup sheet; they're switched off there instead.
    public class PageRenderers : MonoBehaviour
    {
        [SerializeField] AppPages pages = AppPages.All;
        [SerializeField] Renderer[] renderers = new Renderer[0];

        public void ActionOnPageChanged(AppPage page)
        {
            bool isShown = (pages & (AppPages)(1 << (int)page)) != 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = isShown;
            }
        }
    }
}
