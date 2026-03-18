using Tempera.Mental.Input;
using UnityEngine;

namespace Tempera.Mental.Core
{
    public class ApplicationManager : MonoBehaviour
    {
        private float targetAspect = 3.0f / 4.0f; // 720 / 960

        [SerializeField] InputManager inputManager;

        private void Start()
        {
            Application.targetFrameRate = 60;

            UpdateAspectRatio();
        }

        // Use Update if you want it to react instantly to window dragging in a Build
        void Update()
        {
            UpdateAspectRatio();
        }

        void UpdateAspectRatio()
        {
            // Current window aspect ratio
            float windowAspect = (float)Screen.width / (float)Screen.height;

            // How much we need to scale the viewport
            float scaleHeight = windowAspect / targetAspect;

            Camera camera = GetComponent<Camera>();

            // If window is too tall, add letterboxes (horizontal bars)
            if (scaleHeight < 1.0f)
            {
                Rect rect = camera.rect;

                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;

                camera.rect = rect;
            }
            else // If window is too wide, add pillarboxes (vertical bars)
            {
                float scaleWidth = 1.0f / scaleHeight;

                Rect rect = camera.rect;

                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;

                camera.rect = rect;
            }
        }

        public void SetInputEnable(bool isEnabled)
        {
            inputManager.SetEnable(isEnabled);
        }
    }
}
