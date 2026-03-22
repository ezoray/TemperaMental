using TemperaMental.Applications.Config;
using TemperaMental.Input;
using UnityEngine;

namespace TemperaMental.Applications
{
    public class ApplicationManager : MonoBehaviour
    {
  
        private void Start()
        {
            Application.targetFrameRate = ConfigRegistry.App.FrameRate;
        }
    }
}
