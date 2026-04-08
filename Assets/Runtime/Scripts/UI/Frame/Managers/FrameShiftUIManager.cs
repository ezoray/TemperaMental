    using System.Collections.Generic;
    using TemperaMental.Applications.Config;
    using TemperaMental.UI.Core;
    using UnityEngine;
    using UnityEngine.UI;

    namespace TemperaMental.UI.Frames
    {
        public class FrameShiftUIManager : MonoBehaviour
        {
            [SerializeField] List<DimmableRepeatableButton> shiftButtons;

            [SerializeField] Image latchButtonImage;
            [SerializeField] Image wrapButtonImage;

            Color defaultOffColor;
            Color latchOnColor;
            Color wrapOnColor;
            Color shiftOnColor;

            private void Awake()
            {
                defaultOffColor = ConfigRegistry.UI.DefaultColor;
                latchOnColor = ConfigRegistry.UI.GreenColor;
                wrapOnColor = ConfigRegistry.UI.PurpleColor;
                shiftOnColor = ConfigRegistry.UI.CyanColor;
            }

            public void ActionOnShiftButtonLatched(int direction, bool isLatched)
            {
                shiftButtons[direction].image.color = isLatched ? shiftOnColor : defaultOffColor;
            }

            public void ActionOnLatchStateChanged(bool isOn)
            {
                latchButtonImage.color = isOn ? latchOnColor : defaultOffColor;

                foreach (var shiftButton in shiftButtons)
                {
                    shiftButton.SetRepeat(!isOn);

                    if(!isOn)
                    {
                        shiftButton.image.color = defaultOffColor;
                    }
                }
            }

            public void ActionWrapStateChanged(bool isOn)
            {
                wrapButtonImage.color = isOn ? wrapOnColor : defaultOffColor;
            }
        }
    }
