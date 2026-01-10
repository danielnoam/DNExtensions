using UnityEditor;

namespace DNExtensions
{
    public static class PlayFromCameraMenu
    {
        // % = Ctrl (Windows/Linux) or Cmd (Mac)
        // & = Alt (Windows/Linux) or Option (Mac)
        // # = Shift
        // _ followed by a key = Function keys (e.g., _F1 for F1)
        [MenuItem("Tools/Play from Camera Position #%&p", false)]
        private static void PlayFromCameraMenuItem()
        {
            PlayFromCamera.PlayFromCurrentCamera();
        }
    }
}