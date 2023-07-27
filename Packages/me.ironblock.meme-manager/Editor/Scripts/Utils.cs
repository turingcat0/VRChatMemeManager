using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static VRCMemeManager.MemeInfoModel;

namespace VRCMemeManager
{
    public class Utils
    {
        // 获取表情包参数文件
        internal static MenuParameter GetMemeManagerParameter(string avatarId)
        {
            if (avatarId == null)
                return null;
            var path = GetParameterDirPath(avatarId, "/MemeManagerParameter.asset");
            if (File.Exists(path))
            {
                var parameter = AssetDatabase.LoadAssetAtPath(path, typeof(MenuParameter)) as MenuParameter;
                return parameter;
            }
            return null;
        }

        // 获取该模型的参数文件存放位置
        internal static string GetParameterDirPath(string avatarId, string path)
        {
            return "Assets/AvatarData/" + avatarId + "/" + path;
        }

        public static Texture2DArray GifToTextureArray(string path)
        {
            List<Texture2D> array = GetGifFrames(path);
            if (array == null) return null;
            if (array.Count == 0)
            {
                Debug.LogError("Gif is empty or System.Drawing is not working. Try right clicking and reimporting the \"Thry Editor\" Folder!");
                return null;
            }
            Texture2DArray arrayTexture = Textre2DArrayToAsset(array.ToArray());
            return arrayTexture;
        }

        public static List<Texture2D> GetGifFrames(string path)
        {
            List<Texture2D> gifFrames = new List<Texture2D>();
#if SYSTEM_DRAWING
            var gifImage = System.Drawing.Image.FromFile(path);
            var dimension = new System.Drawing.Imaging.FrameDimension(gifImage.FrameDimensionsList[0]);

            int width = Mathf.ClosestPowerOfTwo(gifImage.Width - 1);
            int height = Mathf.ClosestPowerOfTwo(gifImage.Height - 1);

            bool hasAlpha = false;

            int frameCount = gifImage.GetFrameCount(dimension);

            float totalProgress = frameCount * width;
            for (int i = 0; i < frameCount; i++)
            {
                gifImage.SelectActiveFrame(dimension, i);
                var ogframe = new System.Drawing.Bitmap(gifImage.Width, gifImage.Height);
                System.Drawing.Graphics.FromImage(ogframe).DrawImage(gifImage, System.Drawing.Point.Empty);
                var frame = ResizeBitmap(ogframe, width, height);

                Texture2D frameTexture = new Texture2D(frame.Width, frame.Height);

                float doneProgress = i * width;
                for (int x = 0; x < frame.Width; x++)
                {
                    if (x % 20 == 0)
                        if (EditorUtility.DisplayCancelableProgressBar("From GIF", "Frame " + i + ": " + (int)((float)x / width * 100) + "%", (doneProgress + x + 1) / totalProgress))
                        {
                            EditorUtility.ClearProgressBar();
                            return null;
                        }

                    for (int y = 0; y < frame.Height; y++)
                    {
                        System.Drawing.Color sourceColor = frame.GetPixel(x, y);
                        frameTexture.SetPixel(x, frame.Height - 1 - y, new UnityEngine.Color32(sourceColor.R, sourceColor.G, sourceColor.B, sourceColor.A));
                        if (sourceColor.A < 255.0f)
                        {
                            hasAlpha = true;
                        }
                    }
                }

                frameTexture.Apply();
                gifFrames.Add(frameTexture);
            }
            EditorUtility.ClearProgressBar();
            //Debug.Log("has alpha? " + hasAlpha);
            for (int i = 0; i < frameCount; i++)
            {
                EditorUtility.CompressTexture(gifFrames[i], hasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1, UnityEditor.TextureCompressionQuality.Best);
                gifFrames[i].Apply(true, false);
            }
#endif
            return gifFrames;
        }
#if SYSTEM_DRAWING
        public static System.Drawing.Bitmap ResizeBitmap(System.Drawing.Image image, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new System.Drawing.Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = System.Drawing.Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (var wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, System.Drawing.GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
#endif

        private static Texture2DArray Textre2DArrayToAsset(Texture2D[] array)
        {
            Texture2DArray texture2DArray = new Texture2DArray(array[0].width, array[0].height, array.Length, array[0].format, true);

#if SYSTEM_DRAWING
            for (int i = 0; i < array.Length; i++)
            {
                for (int m = 0; m < array[i].mipmapCount; m++)
                {
                    UnityEngine.Graphics.CopyTexture(array[i], 0, m, texture2DArray, i, m);
                }
            }
#endif

            texture2DArray.anisoLevel = array[0].anisoLevel;
            texture2DArray.wrapModeU = array[0].wrapModeU;
            texture2DArray.wrapModeV = array[0].wrapModeV;

            texture2DArray.Apply(false, true);

            return texture2DArray;
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
