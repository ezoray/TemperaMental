using Tempera.Mental.Input;
using UnityEngine;

namespace Tempera.Mental.Applications
{
    public class ApplicationManager : MonoBehaviour
    {
        [SerializeField] InputManager inputManager;

        private void Start()
        {
            Application.targetFrameRate = 60;
        }

        public void SetInputEnable(bool isEnabled)
        {
            inputManager.SetEnable(isEnabled);
        }
    }
}
