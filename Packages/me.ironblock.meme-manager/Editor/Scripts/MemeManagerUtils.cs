using System.Collections.Generic;
using System.IO;
using UnityEditor.AnimatedValues;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using VRC.SDKBase;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Reflection;

namespace VRCMemeManager
{
    public class MemeManagerUtils
    {
        [System.Serializable]
        public class MemeItemInfo
        {
            public string name;
            public string type;
            public Texture2D memeTexture;
            public bool isGIF;
            public int fps;
            public bool keepAspectRatio;

            public AnimBool animBool = new AnimBool { speed = 3.0f };

            public MemeItemInfo(string _name = "新表情", string _type = "")
            {
                name = _name;
                type = _type;
            }
            public MemeItemInfo(MemeManagerParameter.MemeInfo info)
            {
                name = info.name;
                type = info.type;
                memeTexture = info.memeTexture;
                isGIF = info.isGIF;
                fps = info.fps;
                keepAspectRatio = info.keepAspectRatio;
            }
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
                EditorUtility.CompressTexture(gifFrames[i], hasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1, UnityEditor.TextureCompressionQuality.Normal);
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
    

    // 创建表情包参数文件
    internal static MemeManagerParameter CreateMemeManagerParameter(GameObject avatar)
        {
            if (avatar == null)
                return null;
            var parameter = ScriptableObject.CreateInstance<MemeManagerParameter>();
            var avatarId = MoyuToolkitUtils.GetOrCreateAvatarId(avatar);
            parameter.avatarId = avatarId;
            parameter.memeList.Add(new MemeManagerParameter.MemeInfo { name = "表情包1", type = "" });
            var dir = GetParameterDirPath(avatarId, "");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(parameter, GetParameterDirPath(avatarId, "MemeManagerParameter.asset"));
            return parameter;
        }
        // 获取表情包参数文件
        internal static MemeManagerParameter GetMemeManagerParameter(string avatarId)
        {
            if (avatarId == null)
                return null;
            var path = GetParameterDirPath(avatarId, "/MemeManagerParameter.asset");
            if (File.Exists(path))
            {
                var parameter = AssetDatabase.LoadAssetAtPath(path, typeof(MemeManagerParameter)) as MemeManagerParameter;
                return parameter;
            }
            return null;
        }

        // 获取该模型的参数文件存放位置
        internal static string GetParameterDirPath(string avatarId, string path)
        {
            return "Assets/AvatarData/" + avatarId + "/" + path;
        }

        // 通过检测type字段，判断是否为分类模式
        internal static bool HasClassify(List<MemeManagerParameter.MemeInfo> list)
        {
            foreach (var item in list)
                if (item.type != null && item.type.Length > 0)
                    return true;
            return false;
        }
        // 通过检测type字段，判断是否为分类模式
        internal static bool HasClassify(List<MemeItemInfo> list)
        {
            foreach (var item in list)
                if (item.type != null && item.type.Length > 0)
                    return true;
            return false;
        }

        // 应用到模型
        internal static void ApplyToAvatar(GameObject avatar, MemeManagerParameter parameter, int textureAlatlasSize)
        {
            var memeList = new List<MemeManagerParameter.MemeInfo>();
            var _memeList = parameter.memeList;
            foreach (var info in _memeList)
            {
                if (info.memeTexture == null) continue;
                memeList.Add(info);
            }

            var avatarId = MoyuToolkitUtils.GetAvatarId(avatar);
            var dirPath = GetParameterDirPath(avatarId, "");
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            
            var expressionParameters = descriptor.expressionParameters;
            var expressionsMenu = descriptor.expressionsMenu;
            /***添加Particle System ***/
            var memeEmitter = avatar.transform.Find("MemeEmitter");
            if (memeEmitter != null)
            {
                UnityEngine.Object.DestroyImmediate(memeEmitter.gameObject);
            }
            GameObject memeEmitterPrebab = Resources.Load<GameObject>("Prefabs/MemeEmitter");
            memeEmitter = Object.Instantiate(memeEmitterPrebab, avatar.transform).transform;
            memeEmitter.gameObject.name = "MemeEmitter";
            memeEmitter.Translate(new Vector3(0, descriptor.ViewPosition.y, 0));
            ParticleSystem particleSystem = memeEmitter.gameObject.GetComponent<ParticleSystem>();

            //准备目录
            var memeAnimDir = dirPath + "Anim/MemeManager/";
            if (Directory.Exists(memeAnimDir))
                Directory.Delete(memeAnimDir, true);
            Directory.CreateDirectory(memeAnimDir);
            memeAnimDir += "/";
            //检查表情包贴图可读性
            foreach (var item in memeList)
            {
                if (!item.memeTexture.isReadable)
                {
                    Debug.Log(AssetDatabase.GetAssetPath(item.memeTexture) + "的表情包没有设置为脚本可读写, 请修改贴图的导入设置");
                }

            }

            var textureDir = dirPath + "/Textures/MemeManager";
            if (Directory.Exists(textureDir))
                Directory.Delete(textureDir, true);
            Directory.CreateDirectory(textureDir);
            textureDir+= "/";

            //创建表情包贴图
            List<Texture2DArray> texture2DArrayList = new List<Texture2DArray>();
            int arrayLength = 0;
            foreach (var item in memeList)
            {
                Texture2DArray _2darray = null;
                if (item.isGIF)
                {
                    _2darray = GifToTextureArray(AssetDatabase.GetAssetPath(item.memeTexture));
                }
                else {
                    _2darray = new Texture2DArray(item.memeTexture.width, item.memeTexture.height, 1, item.memeTexture.format, item.memeTexture.mipmapCount, true);
                    Graphics.CopyTexture(item.memeTexture, 0, 0, _2darray, 0, 0);
                }
                texture2DArrayList.Add(_2darray);
                AssetDatabase.CreateAsset(_2darray, textureDir + item.name + ".asset");
                arrayLength += _2darray.depth;
            }




            Texture2DArray texture2DArrayAlatlas = new Texture2DArray();


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
           

            //设置材质
            var mat = Resources.Load<Material>("Materials/MemeEmitterMaterial");
            particleSystem.GetComponent<ParticleSystemRenderer>().material = mat;



            AnimationClip disableMemeEmitterAnim = Resources.Load<AnimationClip>("Animations/DisableMemeEmitter");
            AnimationClip enableMemeEmitterAnim = Resources.Load<AnimationClip>("Animations/EnableMemeEmitter"); ;
            //动画控制器
            AnimatorController fxController = null;
            descriptor.customizeAnimationLayers = true;
            descriptor.customExpressions = true;
            var baseAnimationLayers = descriptor.baseAnimationLayers;
            for (var i = 0; i < baseAnimationLayers.Length; i++)
            {
                var layer = descriptor.baseAnimationLayers[i];
                if (layer.type == AnimLayerType.FX)
                {
                    UnityEditor.Animations.AnimatorController controller;
                    if (layer.isDefault || layer.animatorController == null)
                    {

                        controller = Resources.Load<AnimatorController>("VRC/FXLayer");
                        layer.isDefault = false;
                        layer.isEnabled = true;
                        layer.animatorController = controller;
                        baseAnimationLayers[i] = layer;
                    }
                    else
                    {
                        controller = layer.animatorController as AnimatorController;
                    }
                    fxController = controller;
                }
            }

            MoyuToolkitUtils.AddControllerParameter(fxController, "MemeType_Int", AnimatorControllerParameterType.Int);
            if (fxController == null)
            {
                EditorUtility.DisplayDialog("错误", "发生意料之外的情况，请重新设置 AvatarDescriptor 中的 Playable Layers 后再试！", "确认");
                return;
            }

            //设置FX的Layer
            for (var i = 0; i < fxController.layers.Length; i++)
            {
                var layer = fxController.layers[i];
                if (layer.name.StartsWith("MemeEmitter"))
                {
                    fxController.RemoveLayer(i);
                    i--;
                }
            }
            // 添加新StateMachine
            //Layer 1
            var stateMachineParameters = new AnimatorStateMachine()
            {
                name = "MemeEmitterParameter",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachineParameters, AssetDatabase.GetAssetPath(fxController));





            //DisableMemeEmitter
            var disableMemeEmitterState = stateMachineParameters.AddState("DisableMemeEmitter");
            disableMemeEmitterState.motion = disableMemeEmitterAnim;
            stateMachineParameters.defaultState = disableMemeEmitterState;
            //EnableMemeEmitter
            var enableMemeEmitterState = stateMachineParameters.AddState("EnableMemeEmitter");
            enableMemeEmitterState.motion = enableMemeEmitterAnim;
            var parameterDriver = enableMemeEmitterState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "MemeType_Int", value = 0 });
            //其余的
            EditorCurveBinding bindTexture = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "_MainTex",
                type = typeof(ParticleSystemRenderer)
            };
            var trans1 = enableMemeEmitterState.AddTransition(disableMemeEmitterState);
            trans1.hasExitTime = true;
            trans1.exitTime = 2;
            trans1.hasFixedDuration = true;
            trans1.duration = 0;
            trans1.interruptionSource = TransitionInterruptionSource.Destination;
            int j = 0;
            foreach (var item in memeList)
            {
                var animCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, textureArrayNameIndexMap[item.name])});
                var animClip = new AnimationClip();
                animClip.SetCurve("MemeEmitter", typeof(ParticleSystemRenderer), "_MainTex", animCurve);
                AnimationUtility.SetObjectReferenceCurve(animClip, bindTexture, texture2DArrayskeyframes);
                AssetDatabase.CreateAsset(animClip, memeAnimDir + item.name + ".asset");

                var stateTmp = stateMachineParameters.AddState(item.name);
                stateTmp.motion = animClip;
                var trans8 = stateTmp.AddTransition(enableMemeEmitterState);
                trans8.hasExitTime = true;
                trans8.exitTime = 1;
                trans8.hasFixedDuration = true;
                trans8.duration = 0;
                var trans9 = disableMemeEmitterState.AddTransition(stateTmp);
                trans9.hasExitTime = false;
                trans9.exitTime = 0;
                trans9.hasFixedDuration = false;
                trans9.duration = 0;
                trans9.AddCondition(AnimatorConditionMode.Equals, j + 1, "MemeType_Int");
                j++;
            }


            //Timer
            var stateMachineTimer = new AnimatorStateMachine()
            {
                name = "MemeEmitterEmissionTimer",
                hideFlags = HideFlags.HideInHierarchy
            };

            EditorCurveBinding bindTimer = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "material._Timer",
                type = typeof(ParticleSystemRenderer)
            };
            var curveTimer = new AnimationCurve(new Keyframe[] {new Keyframe(0,0), new Keyframe(12000, 12000 * 60)});
            AnimationClip animationClipTimer = new AnimationClip { name = "AnimTimer" };
            AnimationUtility.SetEditorCurve(animationClipTimer, bindTimer, curveTimer);
            AssetDatabase.AddObjectToAsset(stateMachineTimer, AssetDatabase.GetAssetPath(fxController));

            fxController.AddLayer(new AnimatorControllerLayer
            {
                name = stateMachineParameters.name,
                defaultWeight = 1f,
                stateMachine = stateMachineParameters
            });

            fxController.AddLayer(new AnimatorControllerLayer
            {
                name = stateMachineTimer.name,
                defaultWeight = 1f,
                stateMachine = stateMachineTimer
            });






            /*** 配置VRCExpressionParameters ***/
            {
                if (expressionParameters == null || expressionParameters.parameters == null)
                {
                    expressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                    var parameterTemplate = Resources.Load<VRCExpressionParameters>("VRC/Parameters");
                    expressionParameters.parameters = parameterTemplate.parameters;
                    AssetDatabase.CreateAsset(expressionParameters, dirPath + "ExpressionParameters.asset");
                }
                var parameters = expressionParameters.parameters;
                var newParameters = new List<VRCExpressionParameters.Parameter>();
                foreach (var par in parameters)
                    if (!par.name.StartsWith("MemeType_") && par.name != "")
                        newParameters.Add(par);

                newParameters.Add(new VRCExpressionParameters.Parameter
                {
                    name = "MemeType_Int",
                    valueType = VRCExpressionParameters.ValueType.Int,
                    defaultValue = 0,
                    saved = true
                });
                expressionParameters.parameters = newParameters.ToArray();
            }


            /*** 配置VRCExpressionsMenu ***/
            {
                // 创建新Menu文件夹
                var menuDir = dirPath + "Menu/MemeManager";
                if (Directory.Exists(menuDir))
                    Directory.Delete(menuDir, true);
                Directory.CreateDirectory(menuDir);
                menuDir += "/";

                /*** 生成动作菜单 ***/
                var mainMemeMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                {
                    var hasClassify = HasClassify(memeList);
                    // 归类
                    var memeTypeMap = new Dictionary<string, List<MemeManagerParameter.MemeInfo>>();
                    foreach (var info in memeList)
                    {
                        var type = info.type.Length == 0 ? "未分类" : info.type;
                        if (!memeTypeMap.ContainsKey(type))
                            memeTypeMap.Add(type, new List<MemeManagerParameter.MemeInfo>());
                        memeTypeMap[type].Add(info);
                    }

                    var memeMenuMap = new Dictionary<string, VRCExpressionsMenu>();
                    var mainMemeMenuIndex = 0;
                    // 生成类型菜单
                    foreach (var item in memeTypeMap)
                    {
                        var name = item.Key;
                        var infoList = item.Value;

                        var menuList = new List<VRCExpressionsMenu>();
                        var nowMemeMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                        AssetDatabase.CreateAsset(nowMemeMenu, menuDir + "MemeType_" + name + ".asset");
                        EditorUtility.SetDirty(nowMemeMenu);
                        menuList.Add(nowMemeMenu);
                        memeMenuMap.Add(name, nowMemeMenu);

                        // 判断是否已分类
                        if (!hasClassify)
                            mainMemeMenu = nowMemeMenu;

                        if (nowMemeMenu.controls.Count == 7)
                        {
                            var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                            AssetDatabase.CreateAsset(newMenu, menuDir + "MemeType_" + mainMemeMenuIndex++ + ".asset");
                            nowMemeMenu.controls.Add(new VRCExpressionsMenu.Control
                            {
                                name = "下一页",
                                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = newMenu
                            });
                            nowMemeMenu = newMenu;
                            menuList.Add(newMenu);
                            EditorUtility.SetDirty(newMenu);
                        }
                        if (hasClassify)
                            mainMemeMenu.controls.Add(new VRCExpressionsMenu.Control
                            {
                                name = name,
                                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = nowMemeMenu
                            });

                        foreach (var info in infoList)
                        {
                            if (nowMemeMenu.controls.Count == 7)
                            {
                                var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                                AssetDatabase.CreateAsset(newMenu, menuDir + "MemeType_" + name + "_" + (menuList.Count) + ".asset");
                                EditorUtility.SetDirty(newMenu);
                                if (nowMemeMenu != null)
                                {
                                    nowMemeMenu.controls.Add(new VRCExpressionsMenu.Control
                                    {
                                        name = "下一页",
                                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                        subMenu = newMenu
                                    });
                                }
                                menuList.Add(newMenu);
                                nowMemeMenu = newMenu;
                            }
                            nowMemeMenu.controls.Add(new VRCExpressionsMenu.Control
                            {
                                name = info.name,
                                icon = info.memeTexture,
                                type = VRCExpressionsMenu.Control.ControlType.Button,
                                parameter = new VRCExpressionsMenu.Control.Parameter { name = "MemeType_Int" },
                                value = memeList.IndexOf(info) + 1
                            });
                        }
                    }
                    if (hasClassify)
                        AssetDatabase.CreateAsset(mainMemeMenu, menuDir + "ActionMenu.asset");
                }

                // 配置主菜单
                if (expressionsMenu == null)
                    expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                VRCExpressionsMenu.Control memeControl = null;
                // 表情包
                foreach (var control in expressionsMenu.controls)
                {
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                    {
                        if (control.name == "Memes")
                            memeControl = control;
                    }
                }
                if (memeControl == null)
                {
                    expressionsMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = "Memes",
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = mainMemeMenu,
                    });
                }
                else
                {
                    memeControl.subMenu = mainMemeMenu;
                }
                if (AssetDatabase.GetAssetPath(expressionsMenu) == "")
                    AssetDatabase.CreateAsset(expressionsMenu, dirPath + "ExpressionsMenu.asset");
                else
                    EditorUtility.SetDirty(expressionsMenu);
            }

            /*** 应用修改 ***/
            EditorUtility.SetDirty(fxController);
            EditorUtility.SetDirty(expressionsMenu);
            EditorUtility.SetDirty(expressionParameters);
            descriptor.customExpressions = true;
            descriptor.expressionParameters = expressionParameters;
            descriptor.expressionsMenu = expressionsMenu;
            EditorUtility.DisplayDialog("提醒", "应用成功，快上传模型测试下效果吧~", "确认");
        }

    }
}
