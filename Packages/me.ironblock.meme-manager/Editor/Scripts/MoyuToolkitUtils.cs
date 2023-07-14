#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VRCMemeManager
{
    public class MoyuToolkitUtils
    {
        /* ============================================== 控制台 ============================================== */
        public static void Print(params object[] strs)
        {
            string log = "";
            foreach (var str in strs)
            {
                if (log.Length > 0)
                {
                    log += " ";
                }
                log += str ?? "null";
            }
            Debug.Log(log);
        }

        private static MethodInfo clearMethod;
        public static void ClearConsole()
        {
            if (clearMethod == null)
            {
                Type log = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                clearMethod = log.GetMethod("Clear");
            }
            clearMethod.Invoke(null, null);
        }

        /*public static void LookGameObject(GameObject gameObject)
        {
            var view = SceneView.lastActiveSceneView;
            view.rotation = Quaternion.Euler(new Vector3(20, 180, 0));
            view.Repaint();

            Selection.activeGameObject = gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
        }*/

        /* ============================================== 获取工具 ============================================== */
        public static string GetAssetsPath(string path = null)
        {
            /*var _path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<MoyuToolkit>()));
            _path = _path.Substring(0, _path.LastIndexOf("/"));
            _path = _path.Substring(0, _path.LastIndexOf("/") + 1);*/
            var _path = "Packages/cc.moyuer.avatartoolkit/Editor/";
            if (path != null)
            {
                while (path.StartsWith("/"))
                    path = path.Substring(1);
                _path += path;
            }
            return _path;
        }

        public static Dictionary<string, GameObject> GetHumanBoneFromName(GameObject gameObject, string[] boneNames)
        {
            var result = new Dictionary<string, GameObject>();
            foreach (var boneName in boneNames)
            {
                GameObject obj;
                try
                {
                    obj = GetHumanBoneFromName(gameObject, boneName);
                }
                catch (Exception e)
                {
                    Print(e);
                    obj = null;
                }
                result.Add(boneName, obj);
            }
            return result;
        }
        public static GameObject GetHumanBoneFromName(GameObject gameObject, string boneName)
        {
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
                throw new Exception("在根路径下找不到Animator组件");
            var bone = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), boneName);
            var boneTran = animator.GetBoneTransform(bone);
            if (boneTran == null)
                throw new Exception("找不到对应骨骼\"" + bone.ToString() + "\",请检查模型Animator组件是否有Avatar参数、FBX文件的Rig-AnimationType参数是否为Human，以及相应骨骼是否配置。");
            return boneTran.gameObject;
        }
        public static string GetFileType(string path)
        {
            var strs = path.Split('.');
            var type = strs[strs.Length - 1];
            return type.ToLower();
        }

      

        /* ============================================== 工具 ============================================== */

        public static List<T> LinkGameObjectList<T>(List<T> list1, List<T> list2)
        {
            List<T> newList = new List<T>();
            foreach (var obj in list1)
                if (obj != null && !newList.Contains(obj))
                    newList.Add(obj);
            foreach (var obj in list2)
                if (obj != null && !newList.Contains(obj))
                    newList.Add(obj);
            return newList;
        }
        public static void CopyFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    if (!c.EndsWith(".meta"))
                    {
                        string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                        File.Copy(c, destFile, true);//覆盖模式
                    }
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));
                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //采用递归的方法实现
                    CopyFolder(c, destDir);
                });
            }
            else
            {
                Debug.LogError("复制文件时找不到源文件：" + sourcePath);
            }
        }
        /*public Texture2D TextureToTexture2D(Texture texture)
        {
            Texture2D texture2d = texture as Texture2D;
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2d));
            //图片Read/Write Enable的开关
            ti.isReadable = true;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture2d));
            return texture2d;
        }*/

        private static string CreateRandomCode(int len)
        {
            string str = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] chars = str.ToCharArray();
            StringBuilder strRan = new StringBuilder();
            System.Random ran = new System.Random();
            for (int i = 0; i < len; i++)
                strRan.Append(chars[ran.Next(0, 36)]);
            return strRan.ToString();
        }
        public static string GetOrCreateAvatarId(GameObject avatar)
        {
            var id = GetAvatarId(avatar);
            if (id != null)
                return id;
            id = "avatar_" + CreateRandomCode(36);
            //排重
            while (HasTag(id))
                id = "avatar_" + CreateRandomCode(36);
            //添加Tag
            AddTag(id, avatar);
            return id.Substring(7);
        }
        public static string GetAvatarId(GameObject avatar)
        {
            if (avatar == null)
                return null;
            var tag = avatar.tag;
            if (!tag.StartsWith("avatar_") || tag.Length != 43)
                return null;
            return tag.Substring(7);
        }

        private static void AddTag(string tag, GameObject obj)
        {
            if (!HasTag(tag))
            {
                SerializedObject tagManager = new SerializedObject(obj);//序列化物体
                SerializedProperty it = tagManager.GetIterator();//序列化属性
                while (it.NextVisible(true))//下一属性的可见性
                {
                    if (it.name == "m_TagString")
                    {
                        it.stringValue = tag;
                        tagManager.ApplyModifiedProperties();
                    }
                }
            }
        }
        private static bool HasTag(string tag)
        {
            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.tags[i].Contains(tag))
                    return true;
            }
            return false;
        }

        // 移动数组元素的位置
        internal static void MoveListItem<T>(ref List<T> list, int src, int tar)
        {
            var item = list[tar];
            list[tar] = list[src];
            list[src] = item;
        }
        // 添加参数
        internal static void AddControllerParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            bool shouldAddParameters = true;
            for (var a = 0; a < controller.parameters.Length; a++)
            {
                if (controller.parameters[a].name == name)
                {
                    shouldAddParameters = false;
                    controller.parameters[a].type = type;
                    break;
                }
            }
            if (shouldAddParameters)
                controller.AddParameter(name, type);
        }
        // 只保留字符串内的英文和数字
        internal static string GetNumberAlpha(string source)
        {
            string pattern = "[A-Za-z0-9_]";
            string strRet = "";
            MatchCollection results = Regex.Matches(source, pattern);
            foreach (var v in results)
            {
                strRet += v.ToString();
            }
            return strRet;
        }

        public static string GetTransfromPath(GameObject gameObject)
        {
            return GetTransfromPath(gameObject.transform);
        }

        // 获取完整路径
        public static string GetTransfromPath(Transform transform)
        {
            var path = transform.name;
            while (transform != null)
            {
                transform = transform.parent;
                if (transform == null) return path;
                path = transform.name + "/" + path;
            }
            return path;
        }

        // 获取Avatar的Armature名称
        public static string GetAvatarArmatureName(GameObject avatar)
        {
            try
            {
                return avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /* ============================================== 文本读写 ============================================== */
        public static void WriteTxt(string str, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                path = path.Replace("\\", "/");
                var dir = path.Substring(0, path.LastIndexOf("/"));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            var file = File.CreateText(path);
            file.Write(str);
            file.Close();
        }
        public static string ReadTxt(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return null;
        }
    }
}
#endif