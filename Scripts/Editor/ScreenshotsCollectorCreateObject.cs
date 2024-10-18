using UnityEditor;

namespace PlayFi.Editor
{
	internal class ScreenshotsCollectorCreateObject
	{
		[MenuItem("GameObject/Add PlayFiScreenshotsCollector")]
		internal static void AddScreenshotsCollector()
		{
			//as we have self-instantiated singleton that should create an instance if it not exists
			var screenshotsMaker = ScreenshotsCollector.Instance;
		}
	}
}
