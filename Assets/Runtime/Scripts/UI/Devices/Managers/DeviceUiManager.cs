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

        private const string NO_DEVICES_TEXT = "No Devices";

        public void OnDropdownValueChanged(int index)
        {
            if (deviceDropdown.options.Count > 0 && index >= 0 && index < deviceDropdown.options.Count)
            {
                string device = deviceDropdown.options[index].text;

                if (device != NO_DEVICES_TEXT)
                {
                    LogMan.Log($"User selected: {device}");
                    onDeviceChanged?.Invoke(device);
                }
            }
        }

        public void ActionOnDevicesUpdated(List<string> newList)
        {
            string currentSelection = deviceDropdown.options.Count > 0 ? deviceDropdown.options[deviceDropdown.value].text : null;

            deviceDropdown.ClearOptions();

            if (newList == null || newList.Count == 0)
            {
                deviceDropdown.AddOptions(new List<string> { NO_DEVICES_TEXT });
                deviceDropdown.interactable = false;
                deviceDropdown.RefreshShownValue();
                return;
            }

            deviceDropdown.interactable = true;
            deviceDropdown.AddOptions(newList);

            if (currentSelection != null && currentSelection != NO_DEVICES_TEXT)
            {
                int newIndex = deviceDropdown.options.FindIndex(option => option.text == currentSelection);

                if (newIndex != -1)
                {
                    deviceDropdown.SetValueWithoutNotify(newIndex);
                }
                else
                {
                    deviceDropdown.value = 0;
                }
            }

            deviceDropdown.RefreshShownValue();
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
    }
}