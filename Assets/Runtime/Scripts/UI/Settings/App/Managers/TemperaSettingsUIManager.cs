using TemperaMental.Applications.Config;
using TMPro;
using UnityEngine;

namespace TemperaMental.UI.Settings.App
{
    public class TemperaSettingsUIManager : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI controlChannel;
        [SerializeField] TextMeshProUGUI followTempera;

        public void ActionOnControlChannelChanged(int channel)
        {
            controlChannel.text = channel.ToString();
        }

        public void ActionOnFollowTemperaChanged(bool isFollowing)
        {
            followTempera.text = isFollowing ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText;
        }
    }
}
