using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.UI.Core;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Modes
{
    // Switches the contextual mode from the tabs under the display: shows the mode's groups, lights its tab and
    // remembers it. Tabs work in every playback and recording state.
    public class ModeUIManager : MonoBehaviour
    {
        [Header("Order: Play, Transform, Edit, Record, Sound, Settings")]
        [SerializeField] LightableButton[] tabs;

        [Tooltip("Searched for mode groups, inactive ones included")]
        [SerializeField] Transform groupRoot;

        [SerializeField] UnityEvent<AppMode> onModeChanged = new UnityEvent<AppMode>();

        ModeGroup[] groups;
        AppMode mode;
        int modeCount;
        string prefsKey;

        private void Awake()
        {
            prefsKey = ConfigRegistry.UI.AppModePrefsKey;
            modeCount = Enum.GetValues(typeof(AppMode)).Length;
            mode = (AppMode)Mathf.Clamp(PlayerPrefs.GetInt(prefsKey, 0), 0, modeCount - 1);

            groups = groupRoot.GetComponentsInChildren<ModeGroup>(true);

            // before the first frame is drawn, so other modes' controls never flash up
            ApplyGroups();
        }

        // LightableButton picks its colours in Awake
        private void Start()
        {
            ApplyTabs();
            onModeChanged?.Invoke(mode);
        }

        // 0–5 from the tabs, in AppMode order
        public void OnClickMode(int value)
        {
            if (value < 0 || value >= modeCount || (AppMode)value == mode) return;

            mode = (AppMode)value;

            ApplyGroups();
            ApplyTabs();

            PlayerPrefs.SetInt(prefsKey, value);
            PlayerPrefs.Save();

            onModeChanged?.Invoke(mode);
        }

        public AppMode Mode => mode;

        private void ApplyGroups()
        {
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i].Apply(mode);
            }
        }

        private void ApplyTabs()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null) tabs[i].SetLit(i == (int)mode);
            }
        }
    }
}
