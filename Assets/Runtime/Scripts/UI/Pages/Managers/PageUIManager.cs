using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.UI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace TemperaMental.UI.Pages
{
    // Switches pages from the tabs in the header: shows the page's groups, lights its tab and remembers it. Tabs work
    // in every playback and recording state.
    public class PageUIManager : MonoBehaviour
    {
        [Header("Order: Compose, Play, Transform, Record, Setup")]
        [SerializeField] LightableButton[] tabs;

        [Tooltip("Searched for page groups, inactive ones included")]
        [SerializeField] Transform groupRoot;

        [FormerlySerializedAs("onModeChanged")]
        [SerializeField] UnityEvent<AppPage> onPageChanged = new UnityEvent<AppPage>();

        PageGroup[] groups;
        AppPage page;
        int pageCount;
        string prefsKey;

        private void Awake()
        {
            prefsKey = ConfigRegistry.UI.AppPagePrefsKey;
            pageCount = Enum.GetValues(typeof(AppPage)).Length;
            page = (AppPage)Mathf.Clamp(PlayerPrefs.GetInt(prefsKey, 0), 0, pageCount - 1);

            groups = groupRoot.GetComponentsInChildren<PageGroup>(true);

            // before the first frame is drawn, so other pages' controls never flash up
            ApplyGroups();
        }

        // LightableButton picks its colours in Awake
        private void Start()
        {
            ApplyTabs();
            onPageChanged?.Invoke(page);
        }

        // 0–4 from the tabs, in AppPage order
        public void OnClickPage(int value)
        {
            if (value < 0 || value >= pageCount || (AppPage)value == page) return;

            page = (AppPage)value;

            ApplyGroups();
            ApplyTabs();

            PlayerPrefs.SetInt(prefsKey, value);
            PlayerPrefs.Save();

            onPageChanged?.Invoke(page);
        }

        public AppPage Page => page;

        private void ApplyGroups()
        {
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i].Apply(page);
            }
        }

        private void ApplyTabs()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null) tabs[i].SetLit(i == (int)page);
            }
        }
    }
}
