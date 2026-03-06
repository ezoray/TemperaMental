using Tempera.Mental.Input;
using UnityEngine;

namespace Tempera.Mental.Core
{
    public class SceneManager : MonoBehaviour
    {
        [SerializeField] InputManager inputManager;

        public void SetInputEnable(bool isEnabled)
        {
            inputManager.SetEnable(isEnabled);
        }
    }
}
