using System.Collections.Generic;
using Tempera.Mental.Logs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Tempera.Mental.UI.Devices
{
    public class DeviceUiManager : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown deviceDropdown;
        List<string> deviceNames;

        [SerializeField] UnityEvent<string> onDeviceChanged;


        public void OnDropdownValueChanged(int index)
        {
            string device = deviceDropdown.options[index].text;
            onDeviceChanged?.Invoke(device);
        }

        public void OnDeviceAdded(string newItem)
        {
            if (!deviceNames.Contains(newItem))
            {
                deviceNames.Add(newItem);
                RefreshUI();
            }
        }

        public void OnDeviceRemoved(string itemToRemove)
        {
            if (deviceNames.Contains(itemToRemove))
            {
                deviceNames.Remove(itemToRemove);
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            // Save what was selected so the user doesn't lose their place
            string currentSelection = deviceDropdown.options.Count > 0 ? deviceDropdown.options[deviceDropdown.value].text : null;

            deviceDropdown.ClearOptions();
            deviceDropdown.AddOptions(deviceNames);

            // Try to re-select the item if it still exists in the new list
            if (currentSelection != null)
            {
                int newIndex = deviceNames.IndexOf(currentSelection);
                if (newIndex != -1)
                {
                    deviceDropdown.value = newIndex;
                }
            }

            deviceDropdown.RefreshShownValue();
        }

        public void OnDeviceListAvailable(List<string> devices)
        {
            foreach (var device in devices)
            {
                LogMan.Log("OnDeviceListAvailable: " + device);
            }

            if (devices.Count > 0)
            {
                deviceNames = devices;

                deviceDropdown.ClearOptions();
                deviceDropdown.AddOptions(devices);

                deviceDropdown.value = 0;
                deviceDropdown.RefreshShownValue();

                onDeviceChanged?.Invoke(devices[0]); // trigger auto selection of first device

                LogMan.Log("Selected device: " + devices[0]);
            }
        }
    }
}
