using UnityEditor;

namespace VRCMemeManager
{
    public class MemeManager
    {

        [MenuItem("表情包管理器/主面板",false, 101)]
        public static void OpenMainPanel()
        {
            EditorWindow.GetWindow(typeof(MemeManagerView));
        }
    }
}
