using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemperaMental.Input
{
    // assumes keyboard actions are either single key or composite modifier and key
    public class KeybindManager : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActionAsset;

        public static KeybindManager Instance { get; private set; }

        const int BindingRootIndex = 0;
        const int CompositeModifierIndex = 1;
        const int CompositeBindingIndex = 2; 
        const string PlayerPrefsKey = "KeybindOverrides";
        const string MouseMapName = "Mouse";
        const string ModifierLeftCtrl = "<Keyboard>/leftCtrl";
        const string ModifierRightCtrl = "<Keyboard>/rightCtrl";
        const string ModifierCtrl = "<Keyboard>/ctrl";
        const string ModifierLeftShift = "<Keyboard>/leftShift";
        const string ModifierRightShift = "<Keyboard>/rightShift";
        const string ModifierShift = "<Keyboard>/shift";
        const string ModifierLeftAlt = "<Keyboard>/leftAlt";
        const string ModifierRightAlt = "<Keyboard>/rightAlt";
        const string ModifierAlt = "<Keyboard>/alt";
        private const string KeyboardPath = "<Keyboard>";
        private const string BindingAnyKey = "<Keyboard>/anyKey";

        InputActionRebindingExtensions.RebindingOperation currentRebindOperation;
        InputAction currentRebindAction;
        InputBinding[] previousBindings;
        bool isComposite;

        Action<InputAction> onRebindComplete;
        Action<InputAction, string> onRebindConflict;
        Action onRebindCancelled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
#if UNITY_EDITOR
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
#endif
            LoadBindings();
        }

        public void StartRebind(InputAction action, Action<InputAction> onComplete, Action<InputAction, string> onConflict, Action onCancelled)
        {
            if (currentRebindOperation != null) CancelRebind();

            if (action.bindings[BindingRootIndex].isPartOfComposite)
            {
                onConflict?.Invoke(action, null);
                return;
            }

            isComposite = action.bindings[BindingRootIndex].isComposite;

            currentRebindAction = action;

            previousBindings = new InputBinding[action.bindings.Count];
            for (int i = 0; i < action.bindings.Count; i++)
            {
                previousBindings[i] = action.bindings[i];
            }

            onRebindComplete = onComplete;
            onRebindConflict = onConflict;
            onRebindCancelled = onCancelled;

            action.Disable();

            var rebind = action.PerformInteractiveRebinding()
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op => CompleteRebind(action))
                .OnCancel(op => CancelRebindInternal());

            if (isComposite)
            {
                rebind = rebind
                    .WithTargetBinding(CompositeBindingIndex)
                    .WithControlsExcluding(BindingAnyKey)
                    .WithControlsExcluding(ModifierCtrl)
                    .WithControlsExcluding(ModifierLeftCtrl)
                    .WithControlsExcluding(ModifierRightCtrl)
                    .WithControlsExcluding(ModifierShift)
                    .WithControlsExcluding(ModifierLeftShift)
                    .WithControlsExcluding(ModifierRightShift)
                    .WithControlsExcluding(ModifierAlt)
                    .WithControlsExcluding(ModifierLeftAlt)
                    .WithControlsExcluding(ModifierRightAlt)
                    .WithControlsHavingToMatchPath(KeyboardPath);
            }

            currentRebindOperation = rebind.Start();
        }

        public void ResetAllToDefaults()
        {
            CancelRebind();

            foreach (var map in GetBindableMaps())
            {
                foreach (var action in map.actions)
                    action.RemoveAllBindingOverrides();
            }

            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public void ResetToDefault(InputAction action)
        {
            action.RemoveAllBindingOverrides();
            SaveBindings();
        }

        public IEnumerable<InputActionMap> GetBindableMaps()
        {
            foreach (var map in inputActionAsset.actionMaps)
            {
                if (map.name == MouseMapName) continue;

                yield return map;
            }
        }

        public string GetBindingDisplayString(InputAction action)
        {
            return action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        private void CompleteRebind(InputAction action)
        {
            currentRebindOperation.Dispose();
            currentRebindOperation = null;

            if (IsBindingInUse(action, out string conflictActionName))
            {
                for (int i = 0; i < previousBindings.Length; i++)
                {
                    action.ChangeBinding(i).To(previousBindings[i]);
                }

                action.Enable();
                currentRebindAction = null;

                onRebindConflict?.Invoke(action, conflictActionName);
                ClearCallbacks();
                return;
            }

            action.Enable();
            currentRebindAction = null;

            SaveBindings();

            onRebindComplete?.Invoke(action);
            ClearCallbacks();
        }

        private void CancelRebindInternal()
        {
            if (currentRebindOperation == null) return;

            currentRebindOperation.Dispose();
            currentRebindOperation = null;

            currentRebindAction?.Enable();
            currentRebindAction = null;

            onRebindCancelled?.Invoke();
            ClearCallbacks();
        }

        private void CancelRebind()
        {
            if (currentRebindOperation == null) return;
            currentRebindOperation.Cancel();
        }

        private void ClearCallbacks()
        {
            onRebindComplete = null;
            onRebindConflict = null;
            onRebindCancelled = null;
        }

        private bool IsBindingInUse(InputAction reboundAction, out string conflictActionName)
        {
            var (newKey, newHasCtrl, newHasShift, newHasAlt) = GetBindingInfo(reboundAction);

            foreach (var map in GetBindableMaps())
            {
                foreach (var action in map.actions)
                {
                    if (action == reboundAction) continue;

                    var (key, hasCtrl, hasShift, hasAlt) = GetBindingInfo(action);

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(newKey) && key == newKey)
                    {
                        if (hasCtrl == newHasCtrl &&
                            hasShift == newHasShift &&
                            hasAlt == newHasAlt)
                        {
                            conflictActionName = action.name;
                            return true;
                        }
                    }
                }
            }

            conflictActionName = null;
            return false;
        }

        private (string key, bool ctrl, bool shift, bool alt) GetBindingInfo(InputAction action)
        {
            if (action.bindings.Count == 0)
            {
                return (null, false, false, false);
            }

            InputBinding binding = action.bindings[BindingRootIndex];

            if (binding.isComposite)
            {
                string modifierPath = action.bindings[CompositeModifierIndex].effectivePath;
                string key = action.bindings[CompositeBindingIndex].effectivePath;

                bool ctrl = modifierPath == ModifierLeftCtrl || modifierPath == ModifierRightCtrl || modifierPath == ModifierCtrl;
                bool shift = modifierPath == ModifierLeftShift || modifierPath == ModifierRightShift || modifierPath == ModifierShift;
                bool alt = modifierPath == ModifierLeftAlt || modifierPath == ModifierRightAlt || modifierPath == ModifierAlt;

                return (key, ctrl, shift, alt);
            }

            return (binding.effectivePath, false, false, false);
        }

        private void SaveBindings()
        {
            string json = inputActionAsset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

            string json = PlayerPrefs.GetString(PlayerPrefsKey);
            inputActionAsset.LoadBindingOverridesFromJson(json);
        }

        private void OnDestroy()
        {
            CancelRebind();
            if (Instance == this) Instance = null;
        }
    }
}