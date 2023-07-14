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
            }
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
        internal static void ApplyToAvatar(GameObject avatar, MemeManagerParameter parameter)
        {
            MoyuToolkitUtils.ClearConsole();
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
            ParticleSystem particleSystem = memeEmitter.gameObject.GetComponent<ParticleSystem>();

            //准备目录
            var memeAnimDir = dirPath + "Anim/MemeManager/";
            var memeTextureDir = dirPath + "Texture/MemeManager/";
            if (Directory.Exists(memeAnimDir))
                Directory.Delete(memeAnimDir, true);
            Directory.CreateDirectory(memeAnimDir);
            memeAnimDir += "/";
            if (Directory.Exists(memeTextureDir))
                Directory.Delete(memeTextureDir, true);
            Directory.CreateDirectory(memeTextureDir);
            memeTextureDir += "/";
            //创建表情包贴图
            Dictionary<string, int> nameIndexMap = new Dictionary<string, int>();
            Texture2D alatlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);
            int index = 0;
            foreach (var info in memeList)
            {
                var path = AssetDatabase.GetAssetPath(info.memeTexture);
                if (path.Contains(".gif"))
                {
                    Debug.Log("path is a gif:" + path);
                }

                int width = info.memeTexture.width;
                int height = info.memeTexture.height;
                Color32[] pixels = info.memeTexture.GetPixels32();
                Color32[] result = new Color32[512 * 512];
                ScaleTo512x512(pixels, width, height, result);
                alatlas.SetPixels32((index % 4) * 512, (index / 4) * 512, 512, 512, result);
                nameIndexMap.Add(info.name, index);
                index++;
            }
            alatlas.Apply(true, false);
            alatlas.Compress(false);

            AssetDatabase.CreateAsset(alatlas, memeTextureDir + "MemeAlatlas.asset");

            AssetDatabase.Refresh();
            //设置材质
            var mat = Resources.Load<Material>("Materials/MemeEmitterMaterial");
            particleSystem.GetComponent<ParticleSystemRenderer>().material = mat;
            mat.mainTexture = alatlas;
            mat.mainTextureScale = new Vector2(0, 0);
            mat.mainTextureOffset = new Vector2(0, 0);
            mat.SetFloat("_TextureU1", 0.0f);
            mat.SetFloat("_TextureV1", 0.0f);
            mat.SetFloat("_TextureU2", 1.0f);
            mat.SetFloat("_TextureV2", 1.0f);



            //读入动画
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
            var stateMachineEmission = new AnimatorStateMachine()
            {
                name = "MemeEmitterEmission",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachineEmission, AssetDatabase.GetAssetPath(fxController));
            //DisableMemeEmitter
            var disableMemeEmitterState = stateMachineEmission.AddState("DisableMemeEmitter");
            disableMemeEmitterState.motion = disableMemeEmitterAnim;
            stateMachineEmission.defaultState = disableMemeEmitterState;
            //EnableMemeEmitter
            var enableMemeEmitterState = stateMachineEmission.AddState("EnableMemeEmitter");
            enableMemeEmitterState.motion = enableMemeEmitterAnim;
            var parameterDriver = enableMemeEmitterState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter() { type = VRC_AvatarParameterDriver.ChangeType.Add, name = "MemeType_Int", value = 128 });

            //Transitions
            var trans1 = disableMemeEmitterState.AddTransition(enableMemeEmitterState);
            trans1.hasExitTime = false;
            trans1.exitTime = 0;
            trans1.hasFixedDuration = true;
            trans1.duration = 0;
            trans1.AddCondition(AnimatorConditionMode.Less, 128, "MemeType_Int");
            trans1.AddCondition(AnimatorConditionMode.NotEqual, 0, "MemeType_Int");

            var trans2 = enableMemeEmitterState.AddTransition(disableMemeEmitterState);
            trans2.hasExitTime = true;
            trans2.exitTime = 1;
            trans2.hasFixedDuration = true;
            trans2.duration = 0;





            var stateMachineUV = new AnimatorStateMachine()
            {
                name = "MemeEmitterEmissionUV",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachineUV, AssetDatabase.GetAssetPath(fxController));

            var idleState = stateMachineUV.AddState("Idle");
            stateMachineUV.defaultState = idleState;

            //Anim States
            Dictionary<string, AnimatorState> nameAnimStateMap = new Dictionary<string, AnimatorState>();
            foreach (var item in nameIndexMap)
            {

                int row = Mathf.FloorToInt(item.Value / 4);
                var curveU1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, (item.Value % 4) * 0.25f), new Keyframe(1f, (item.Value % 4) * 0.25f) });
                var curveU2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, ((item.Value % 4) + 1) * 0.25f), new Keyframe(1f, ((item.Value % 4) + 1) * 0.25f) });
                var curveV1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, (row * 0.25f)), new Keyframe(1f, (row * 0.25f)) });
                var curveV2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, ((row + 1) * 0.25f)), new Keyframe(1f, ((row + 1) * 0.25f)) });
                AnimationClip animationClip = new AnimationClip { name = "Anim" + item.Value };
                EditorCurveBinding bindU1 = new EditorCurveBinding
                {
                    path = "MemeEmitter",
                    propertyName = "material._TextureU1",
                    type = typeof(ParticleSystemRenderer)
                };
                EditorCurveBinding bindU2 = new EditorCurveBinding
                {
                    path = "MemeEmitter",
                    propertyName = "material._TextureU2",
                    type = typeof(ParticleSystemRenderer)
                };
                EditorCurveBinding bindV1 = new EditorCurveBinding
                {
                    path = "MemeEmitter",
                    propertyName = "material._TextureV1",
                    type = typeof(ParticleSystemRenderer)
                };
                EditorCurveBinding bindV2 = new EditorCurveBinding
                {
                    path = "MemeEmitter",
                    propertyName = "material._TextureV2",
                    type = typeof(ParticleSystemRenderer)
                };
                AnimationUtility.SetEditorCurve(animationClip, bindU1, curveU1);
                AnimationUtility.SetEditorCurve(animationClip, bindU2, curveU2);
                AnimationUtility.SetEditorCurve(animationClip, bindV1, curveV1);
                AnimationUtility.SetEditorCurve(animationClip, bindV2, curveV2);
                var settting = AnimationUtility.GetAnimationClipSettings(animationClip);
                settting.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(animationClip, settting);
                AssetDatabase.CreateAsset(animationClip, memeAnimDir + "Anim" + item.Value + ".anim");



                var animState = stateMachineUV.AddState("Anim" + item.Value);
                animState.motion = animationClip;
                nameAnimStateMap[item.Key] = animState;
            }
            AssetDatabase.Refresh();
            //Transitions
            foreach (var item in nameAnimStateMap)
            {
                var trans4 = idleState.AddTransition(item.Value);
                trans4.hasExitTime = false;
                trans4.exitTime = 0;
                trans4.hasFixedDuration = true;
                trans4.duration = 0;
                trans4.AddCondition(AnimatorConditionMode.Equals, nameIndexMap[item.Key] + 1, "MemeType_Int");

                var trans5 = idleState.AddTransition(item.Value);
                trans5.hasExitTime = false;
                trans5.exitTime = 0;
                trans5.hasFixedDuration = true;
                trans5.duration = 0;
                trans5.AddCondition(AnimatorConditionMode.Equals, nameIndexMap[item.Key] + 129, "MemeType_Int");

                var trans6 = item.Value.AddTransition(idleState);
                trans6.hasExitTime = false;
                trans6.exitTime = 0;
                trans6.hasFixedDuration = true;
                trans6.duration = int.MaxValue;
                trans6.interruptionSource = TransitionInterruptionSource.Destination;
                trans6.AddCondition(AnimatorConditionMode.Less, 128, "MemeType_Int");
                trans6.AddCondition(AnimatorConditionMode.NotEqual, 0, "MemeType_Int");
            }

            fxController.AddLayer(new AnimatorControllerLayer
            {
                name = stateMachineEmission.name,
                defaultWeight = 1f,
                stateMachine = stateMachineEmission
            });

            fxController.AddLayer(new AnimatorControllerLayer
            {
                name = stateMachineUV.name,
                defaultWeight = 1f,
                stateMachine = stateMachineUV
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


        /***将input图像缩放到512x512并放到output里***/
        private static void ScaleTo512x512(in Color32[] input, int inputWidth, int inputHeight, Color32[] output)
        {
            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    float xInput = i * (inputWidth - 1) / 512.0f;
                    float yInput = j * (inputHeight - 1) / 512.0f;
                    int xInputFloor = (int)Mathf.Floor(xInput);
                    int yInputFloor = (int)Mathf.Floor(yInput);
                    int xInputCeil = xInputFloor + 1;
                    int yInputCeil = yInputFloor + 1;
                    float xDelta = xInput - xInputFloor;
                    float yDelta = yInput - yInputFloor;
                    try
                    {
                        //bilinear
                        Color32 x1 = add(mul(input[yInputFloor * inputWidth + xInputFloor], (1 - xDelta)),
                            mul(input[(yInputFloor * inputWidth) + xInputCeil], (xDelta)));
                        Color32 x2 = add(mul(input[yInputCeil * inputWidth + xInputFloor], (1 - xDelta)),
                            mul(input[(yInputCeil * inputWidth) + xInputCeil], (xDelta)));
                        output[j * 512 + i] = add(mul(x1, (1 - yDelta)), mul(x2, (yDelta)));
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        Debug.Log("index out of bound:" + "i:" + i + ",j:" + j + ",inputWidth:" + inputWidth + ", inputHeight:" + inputHeight
                            + ",xInput:" + xInput + ",yInput:" + yInput + ",xInputFloor:" + xInputFloor + ",yInputFloor:" + yInputFloor
                            + ",xInputCeil:" + xInputCeil + ",yInputCeil:" + yInputCeil
                            + ",xDelta:" + xDelta + ",yDelta:" + yDelta
                            );
                    }
                }
            }



        }


        public static Color32 mul(Color32 color, float f)
        {
            return new Color32((byte)(color.r * f), (byte)(color.g * f), (byte)(color.b * f), (byte)(color.a * f));
        }
        public static Color32 add(Color32 color1, Color32 color2)
        {
            return new Color32((byte)(color1.r + color2.r), (byte)(color1.g + color2.g), (byte)(color1.b + color2.b), (byte)(color1.a + color2.a));
        }
    }
}
