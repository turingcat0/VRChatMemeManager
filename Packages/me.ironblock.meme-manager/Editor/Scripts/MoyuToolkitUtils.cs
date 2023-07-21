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
    }
}
#endif