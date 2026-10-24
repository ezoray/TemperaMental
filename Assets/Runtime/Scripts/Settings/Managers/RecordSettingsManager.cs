using System;
using TemperaMental.Applications.Config;
using TemperaMental.Core;
using TemperaMental.Logs;
using UnityEngine;
using UnityEngine.Events;

namespace TemperaMental.Settings
{
    // what the Record button records and how: source, trigger, mode, starting state and length. Each setting is
    // stepped through its choices by one button. Saved in PlayerPrefs as soon as they change
    public class RecordSettingsManager : MonoBehaviour
    {
        int[] lengths;

        // read in Awake: the view can close (and save) as the app quits, after the config has gone
        string sourceKey;
        string triggerKey;
        string modeKey;
        string startStateKey;
        string lengthKey;

        RecordSource source;
        RecordTrigger trigger;
        RecordMode mode;
        RecordStartState startState;
        int lengthIndex;

        [SerializeField] UnityEvent<RecordSource> onSourceChanged = new UnityEvent<RecordSource>();
        [SerializeField] UnityEvent<RecordTrigger> onTriggerChanged = new UnityEvent<RecordTrigger>();
        [SerializeField] UnityEvent<RecordMode> onModeChanged = new UnityEvent<RecordMode>();
        [SerializeField] UnityEvent<RecordStartState> onStartStateChanged = new UnityEvent<RecordStartState>();

        // in frames (beats), 0 for no limit
        [SerializeField] UnityEvent<int> onLengthChanged = new UnityEvent<int>();

        private void Awake()
        {
            MidiConfig config = ConfigRegistry.Midi;
            lengths = config.RecordLengths != null && config.RecordLengths.Length > 0 ? config.RecordLengths : new[] { 0 };

            sourceKey = config.RecordSourcePrefsKey;
            triggerKey = config.RecordTriggerPrefsKey;
            modeKey = config.RecordModePrefsKey;
            startStateKey = config.RecordStartStatePrefsKey;
            lengthKey = config.RecordLengthPrefsKey;

            source = Load(sourceKey, (RecordSource)config.DefaultRecordSource);
            trigger = Load(triggerKey, (RecordTrigger)config.DefaultRecordTrigger);
            mode = Load(modeKey, (RecordMode)config.DefaultRecordMode);
            startState = Load(startStateKey, (RecordStartState)config.DefaultRecordStartState);
            lengthIndex = Mathf.Clamp(PlayerPrefs.GetInt(lengthKey, config.DefaultRecordLengthIndex), 0, lengths.Length - 1);
        }

        // raised in Start rather than Awake so every listener has finished its own Awake
        private void Start()
        {
            onSourceChanged?.Invoke(source);
            onTriggerChanged?.Invoke(trigger);
            onModeChanged?.Invoke(mode);
            onStartStateChanged?.Invoke(startState);
            onLengthChanged?.Invoke(lengths[lengthIndex]);
        }

        public void StepSource()
        {
            source = Next(source);
            onSourceChanged?.Invoke(source);
        }

        public void StepTrigger()
        {
            trigger = Next(trigger);
            onTriggerChanged?.Invoke(trigger);
        }

        public void StepMode()
        {
            mode = Next(mode);
            onModeChanged?.Invoke(mode);
        }

        public void StepStartState()
        {
            startState = Next(startState);
            onStartStateChanged?.Invoke(startState);
        }

        public void StepLength()
        {
            lengthIndex = (lengthIndex + 1) % lengths.Length;
            onLengthChanged?.Invoke(lengths[lengthIndex]);
        }

        public void SaveRecordSettings()
        {
            PlayerPrefs.SetInt(sourceKey, (int)source);
            PlayerPrefs.SetInt(triggerKey, (int)trigger);
            PlayerPrefs.SetInt(modeKey, (int)mode);
            PlayerPrefs.SetInt(startStateKey, (int)startState);
            PlayerPrefs.SetInt(lengthKey, lengthIndex);
            PlayerPrefs.Save();

            LogMan.LogTemp($"Record {source}, {trigger}, {mode}, {startState}");
        }

        private static T Load<T>(string key, T fallback) where T : struct, Enum
        {
            int value = PlayerPrefs.GetInt(key, Convert.ToInt32(fallback));

            return Enum.IsDefined(typeof(T), value) ? (T)Enum.ToObject(typeof(T), value) : fallback;
        }

        private static T Next<T>(T value) where T : struct, Enum
        {
            int count = Enum.GetValues(typeof(T)).Length;

            return (T)Enum.ToObject(typeof(T), (Convert.ToInt32(value) + 1) % count);
        }
    }
}
