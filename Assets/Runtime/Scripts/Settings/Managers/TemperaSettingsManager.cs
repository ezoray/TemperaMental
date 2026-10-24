using TemperaMental.Applications.Config;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    // the Tempera's Control MIDI channel (where its emitter notes arrive) and whether the app follows changes made on
    // the hardware. Saved in PlayerPrefs as soon as they change
    public class TemperaSettingsManager : MonoBehaviour
    {
        string controlChannelPrefsKey;
        string followPrefsKey;
        int channelCount;

        int controlChannel;
        bool isFollowing;

        [SerializeField] UnityEvent<int> onControlChannelChanged = new UnityEvent<int>();
        [SerializeField] UnityEvent<bool> onFollowTemperaChanged = new UnityEvent<bool>();

        private void Awake()
        {
            controlChannelPrefsKey = ConfigRegistry.Midi.ControlChannelPrefsKey;
            followPrefsKey = ConfigRegistry.Midi.FollowTemperaPrefsKey;
            channelCount = ConfigRegistry.Midi.MidiChannelCount;

            controlChannel = Mathf.Clamp(PlayerPrefs.GetInt(controlChannelPrefsKey, ConfigRegistry.Midi.DefaultControlChannel), 1, channelCount);
            isFollowing = PlayerPrefs.GetInt(followPrefsKey, ConfigRegistry.Midi.DefaultFollowTempera ? 1 : 0) == 1;
        }

        // raised in Start rather than Awake so every listener has finished its own Awake
        private void Start()
        {
            onControlChannelChanged?.Invoke(controlChannel);
            onFollowTemperaChanged?.Invoke(isFollowing);
        }

        public void StepControlChannel(int directionValue)
        {
            controlChannel = Mathf.Clamp(controlChannel + directionValue, 1, channelCount);

            onControlChannelChanged?.Invoke(controlChannel);
        }

        public void ToggleFollowTempera() => SetFollowTempera(!isFollowing);

        public void SetFollowTempera(bool isFollow)
        {
            if (isFollowing == isFollow) return;

            isFollowing = isFollow;

            onFollowTemperaChanged?.Invoke(isFollowing);
        }

        public void SaveTemperaSettings()
        {
            LogMan.LogTemp($"Control channel {controlChannel}, Follow Tempera {(isFollowing ? ConfigRegistry.UI.OnText : ConfigRegistry.UI.OffText)}");

            PlayerPrefs.SetInt(controlChannelPrefsKey, controlChannel);
            PlayerPrefs.SetInt(followPrefsKey, isFollowing ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
