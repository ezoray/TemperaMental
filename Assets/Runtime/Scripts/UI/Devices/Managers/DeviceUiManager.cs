using System.Collections.Generic;
using TemperaMental.Logs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.UI.Devices
{
    public class DeviceUIManager : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown deviceDropdown;
        [SerializeField] UnityEvent<string> onDeviceChanged;

        // option placeholders
        const string NO_DEVICES_TEXT = "No Devices";
        const string SELECT_DEVICE_TEXT = "Select Device";

        public void OnDropdownValueChanged(int index)
        {
            if (deviceDropdown.options.Count == 0 || index < 0 || index >= deviceDropdown.options.Count) return;

            string device = deviceDropdown.options[index].text;

            if (device == NO_DEVICES_TEXT || device == SELECT_DEVICE_TEXT) return;

            int selectIndex = deviceDropdown.options.FindIndex(option => option.text == SELECT_DEVICE_TEXT);

            // remove placeholder option
            if (selectIndex != -1)
            {
                deviceDropdown.options.RemoveAt(selectIndex);
   
                int adjustedIndex = index - 1;
                deviceDropdown.SetValueWithoutNotify(adjustedIndex);
                deviceDropdown.RefreshShownValue();
            }

            LogMan.Log($"User selected: {device}");
            onDeviceChanged?.Invoke(device);
        }

        public void ActionOnAutoSelectDevice(string deviceName)
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
                deviceDropdown.AddOptions(new List<string> { NO_DEVICES_TEXT });
                deviceDropdown.interactable = false;
                deviceDropdown.SetValueWithoutNotify(0);
                deviceDropdown.RefreshShownValue();
                return;
            }

            deviceDropdown.interactable = true;

            List<string> options = new List<string> { SELECT_DEVICE_TEXT };
            options.AddRange(newList);

            deviceDropdown.AddOptions(options);
            deviceDropdown.SetValueWithoutNotify(0);
            deviceDropdown.RefreshShownValue();
        }
    }
}