using UnityEditor;

namespace VRCMemeManager
{
    public class MemeManager
    {

        [MenuItem("MemeManager/MainPanel",false, 101)]
        public static void OpenMainPanel()
        {
            EditorWindow.GetWindow(typeof(MemeManagerView));
        }
    }
}
