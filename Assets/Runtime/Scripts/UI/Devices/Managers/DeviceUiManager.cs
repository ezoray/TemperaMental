using System.Collections.Generic;
using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Devices
{
    public class DeviceUIManager : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown deviceDropdown;
        [SerializeField] UnityEvent<string> onDeviceChanged;

        string noDevicesText;
        string selectDeviceText;

        private void Awake()
        {
            noDevicesText = ConfigRegistry.UI.NoDevicesText;
            selectDeviceText = ConfigRegistry.UI.SelectDeviceText;
        }

        public void OnDropdownValueChanged(int index)
        {
            if (deviceDropdown.options.Count == 0 || index < 0 || index >= deviceDropdown.options.Count) return;

            string device = deviceDropdown.options[index].text;

            if (device == noDevicesText || device == selectDeviceText) return;

            int selectIndex = deviceDropdown.options.FindIndex(option => option.text == selectDeviceText);

            // remove placeholder option
            if (selectIndex != -1)
            {
                deviceDropdown.options.RemoveAt(selectIndex);
   
                int adjustedIndex = index - 1;
                deviceDropdown.SetValueWithoutNotify(adjustedIndex);
                deviceDropdown.RefreshShownValue();
            }

            onDeviceChanged?.Invoke(device);
        }

        public void ActionOnInitialDeviceFound(string deviceName)
        {
            int index = deviceDropdown.options.FindIndex(option => option.text == deviceName);

            if (index != -1)
            {
                deviceDropdown.value = index;
                deviceDropdown.RefreshShownValue();
            }
        }

        public void ActionOnDevicesUpdated(List<string> newList)
        {
            deviceDropdown.ClearOptions();

            if (newList == null || newList.Count == 0)
            {
                deviceDropdown.AddOptions(new List<string> { noDevicesText });
                deviceDropdown.interactable = false;
                deviceDropdown.SetValueWithoutNotify(0);
                deviceDropdown.RefreshShownValue();
                return;
            }

            deviceDropdown.interactable = true;

            List<string> options = new List<string> { selectDeviceText };
            options.AddRange(newList);

            deviceDropdown.AddOptions(options);
            deviceDropdown.SetValueWithoutNotify(0);
            deviceDropdown.RefreshShownValue();
        }
    }
}