namespace TemperaMental.Applications.Core
{
    using TemperaMental.Applications.Config;
    using UnityEngine;

    public class AspectCorrectionService : MonoBehaviour
    {
        float targetRatio = 3f / 4f;

        int minWidth = 540;
        int minHeight = 720;

        int lastWidth;
        int lastHeight;
        int lastCorrectedWidth;
        int lastCorrectedHeight;

        float resizeDelay = 0.75f;
        float resizeTimer = 0f;

        private void Awake()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            targetRatio = ConfigRegistry.App.TargetRatio;
            minWidth = ConfigRegistry.App.MinWidth;
            minHeight = ConfigRegistry.App.MinHeight;
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                bool isOurCorrection = Screen.width == lastCorrectedWidth &&
                                       Screen.height == lastCorrectedHeight;

                if (!isOurCorrection)
                {
                    lastWidth = Screen.width;
                    lastHeight = Screen.height;
                    resizeTimer = resizeDelay;
                }
                else
                {
                    lastWidth = Screen.width;
                    lastHeight = Screen.height;
                }
            }

            if (resizeTimer > 0f)
            {
                resizeTimer -= Time.deltaTime;
                if (resizeTimer <= 0f)
                    CorrectAspect();
            }
        }

        void CorrectAspect()
        {
            int width = Screen.width;
            int height = Screen.height;

            float currentRatio = (float)width / height;

            if (Mathf.Abs(currentRatio - targetRatio) < 0.01f)
                return;

            int heightFromWidth = Mathf.RoundToInt(width / targetRatio);
            int widthFromHeight = Mathf.RoundToInt(height * targetRatio);

            int newWidth, newHeight;

            if (Mathf.Abs(heightFromWidth - height) < Mathf.Abs(widthFromHeight - width))
            {
                newWidth = width;
                newHeight = heightFromWidth;
            }
            else
            {
                newWidth = widthFromHeight;
                newHeight = height;
            }

            if (newWidth < minWidth)
            {
                newWidth = minWidth;
                newHeight = Mathf.RoundToInt(newWidth / targetRatio);
            }

            if (newHeight < minHeight)
            {
                newHeight = minHeight;
                newWidth = Mathf.RoundToInt(newHeight * targetRatio);
            }

            lastCorrectedWidth = newWidth;
            lastCorrectedHeight = newHeight;

            Screen.SetResolution(newWidth, newHeight, false);

            lastWidth = newWidth;
            lastHeight = newHeight;
        }
    }
}
