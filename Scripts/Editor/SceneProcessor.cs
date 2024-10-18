using UnityEditor.Callbacks;

namespace PlayFi.Editor
{
    internal static class SceneProcessor
    {
        [PostProcessSceneAttribute(int.MaxValue)]
        internal static void OnPostProcessScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0)
                 ScreenshotsCollectorCreateObject.AddScreenshotsCollector();
        }
    }
}