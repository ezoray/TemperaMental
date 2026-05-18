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

        // Track exactly which dimensions changed during this resize cycle
        bool widthChanged = false;
        bool heightChanged = false;

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
                    // Track which frame boundaries were dragged by the user
                    if (Screen.width != lastWidth) widthChanged = true;
                    if (Screen.height != lastHeight) heightChanged = true;

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

            // If it's already close enough, reset tracking flags and exit
            if (Mathf.Abs(currentRatio - targetRatio) < 0.01f)
            {
                ResetTrackingFlags();
                return;
            }

            int newWidth, newHeight;

            // Explicit, historical determination of layout shifts
            if (widthChanged && !heightChanged)
            {
                // Grabbing side borders: Calculate height based on user's new width
                newWidth = width;
                newHeight = Mathf.RoundToInt(width / targetRatio);
            }
            else if (heightChanged && !widthChanged)
            {
                // Grabbing top/bottom borders: Calculate width based on user's new height
                newWidth = Mathf.RoundToInt(height * targetRatio);
                newHeight = height;
            }
            else
            {
                // Diagonal/corner drag, or fallback: Use closest distance heuristic
                int heightFromWidth = Mathf.RoundToInt(width / targetRatio);
                int widthFromHeight = Mathf.RoundToInt(height * targetRatio);

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
            }

            // Enforce layout constraints
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

            ResetTrackingFlags();
        }

        private void ResetTrackingFlags()
        {
            widthChanged = false;
            heightChanged = false;
        }
    }
}