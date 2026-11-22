using UnityEngine;
namespace Hakaton.Demo
{
    public static class DemoPreferences
    {
        public const string VolumeKey="HouseRemembers.MasterVolume";
        public const string MouseKey="HouseRemembers.MouseSensitivity";
        public static float Volume=>Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,.8f));
        public static float MouseSensitivity=>Mathf.Clamp(PlayerPrefs.GetFloat(MouseKey,.11f),.03f,.3f);
        public static void SetVolume(float value){PlayerPrefs.SetFloat(VolumeKey,Mathf.Clamp01(value));AudioListener.volume=Mathf.Clamp01(value);}
        public static void SetMouse(float value){PlayerPrefs.SetFloat(MouseKey,Mathf.Clamp(value,.03f,.3f));}
    }
}
