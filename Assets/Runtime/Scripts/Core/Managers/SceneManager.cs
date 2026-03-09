using Tempera.Mental.Input;
using UnityEngine;

namespace Tempera.Mental.Core
{
    public class SceneManager : MonoBehaviour
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
