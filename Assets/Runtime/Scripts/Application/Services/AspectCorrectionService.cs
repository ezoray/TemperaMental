using UnityEngine;

namespace Tempera.Mental.Applications
{
    using UnityEngine;

    public class AspectCorrectionService : MonoBehaviour
    {
        const float TARGET_RATIO = 3f / 4f;

        const int MIN_WIDTH = 540;
        const int MIN_HEIGHT = 720;

        int lastWidth;
        int lastHeight;
        int lastCorrectedWidth;
        int lastCorrectedHeight;

        float resizeDelay = 0.75f;
        float resizeTimer = 0f;

        void Start()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
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

            if (Mathf.Abs(currentRatio - TARGET_RATIO) < 0.01f)
                return;

            int heightFromWidth = Mathf.RoundToInt(width / TARGET_RATIO);
            int widthFromHeight = Mathf.RoundToInt(height * TARGET_RATIO);

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

            if (newWidth < MIN_WIDTH)
            {
                newWidth = MIN_WIDTH;
                newHeight = Mathf.RoundToInt(newWidth / TARGET_RATIO);
            }

            if (newHeight < MIN_HEIGHT)
            {
                newHeight = MIN_HEIGHT;
                newWidth = Mathf.RoundToInt(newHeight * TARGET_RATIO);
            }

            lastCorrectedWidth = newWidth;
            lastCorrectedHeight = newHeight;

            Screen.SetResolution(newWidth, newHeight, false);

            lastWidth = newWidth;
            lastHeight = newHeight;
        }
    }
}
