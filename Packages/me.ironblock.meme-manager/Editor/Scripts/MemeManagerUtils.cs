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


            //创建表情包贴图
            Dictionary<string, int> nameIndexMap = new Dictionary<string, int>();
            Texture2D alatlas = new Texture2D(textureAlatlasSize, textureAlatlasSize, TextureFormat.RGBA32, true);
            int index = 0;
            Dictionary<string, int> gifNameFramesCountMap = new Dictionary<string, int>();
            Dictionary<string, int> nameArrayIndexMap = new Dictionary<string, int>();
            List<Texture2D> texture2DList = new List<Texture2D>();

            foreach (var info in memeList)
            {
                nameArrayIndexMap.Add(info.name, texture2DList.Count);
                nameIndexMap.Add(info.name, index);
                index++;
                if (info.isGIF)
                {
                    var path = AssetDatabase.GetAssetPath(info.memeTexture);
                    Debug.Log(path);

                    int num = 0;
                    string folder = Path.GetDirectoryName(path); // 获取纹理所属的文件夹路径
                    if (Directory.Exists(folder)) // 判断文件夹是否存在
                    {
                        string[] files = Directory.GetFiles(folder); // 获取文件夹中所有png格式的文件名
                        
                        foreach (var assetPath in files)
                        {
                            if (!assetPath.EndsWith("meta"))
                            {
                                texture2DList.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath));
                                num++;
                            }
                        }
                    }
                    gifNameFramesCountMap.Add(info.name, num);
                }
                else
                {
                    texture2DList.Add(info.memeTexture);
                }
            }
            Rect[] alatlasRects = alatlas.PackTextures(texture2DList.ToArray(), 0, textureAlatlasSize, false);
            var textureDir = dirPath + "/Textures/MemeManager";
            if (Directory.Exists(textureDir))
                Directory.Delete(textureDir, true);
            Directory.CreateDirectory(textureDir);
            textureDir += "/textureAlatlas.asset";
            alatlas.Compress(false);
            AssetDatabase.CreateAsset(alatlas, textureDir);
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
                name = "MemeEmitterEmissionParameters",
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(stateMachineUV, AssetDatabase.GetAssetPath(fxController));

            var idleState = stateMachineUV.AddState("Idle");
            stateMachineUV.defaultState = idleState;

            //Anim States
            Dictionary<string, AnimatorState> nameAnimStateMap = new Dictionary<string, AnimatorState>();
            Dictionary<string, List<AnimatorState>> nameGifAnimStateMap = new Dictionary<string, List<AnimatorState>>();
            foreach (var item in memeList)
            {
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
                EditorCurveBinding bindAspectRatio = new EditorCurveBinding
                {
                    path = "MemeEmitter",
                    propertyName = "material._AspectRatio",
                    type = typeof(ParticleSystemRenderer)
                };
                if (item.isGIF)
                {
                    int num = gifNameFramesCountMap[item.name];
                    List<AnimatorState> animatorStates = new List<AnimatorState>();
                    
                    for (int i = 0; i < num; i++)
                    {
                        float u1 = alatlasRects[nameArrayIndexMap[item.name] + i].xMin;
                        float v1 = alatlasRects[nameArrayIndexMap[item.name] + i].yMin;
                        float u2 = alatlasRects[nameArrayIndexMap[item.name] + i].xMax;
                        float v2 = alatlasRects[nameArrayIndexMap[item.name] + i].yMax;
                        float aspectRatio = item.keepAspectRatio ? (v2 - v1) / (u2 - u1) : 1;
                        var curveU1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, u1), new Keyframe(1.0f/item.fps, u1) }); 
                        var curveU2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, u2), new Keyframe(1.0f / item.fps, u2) });
                        var curveV1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, v1), new Keyframe(1.0f / item.fps, v1) });
                        var curveV2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, v2), new Keyframe(1.0f / item.fps, v2) });
                        var curveRatio = new AnimationCurve(new Keyframe[] { new Keyframe(0, aspectRatio), new Keyframe(1.0f / item.fps, aspectRatio) });
                        AnimationClip animationClip = new AnimationClip { name = "Anim" + item.name + i};
                        AnimationUtility.SetEditorCurve(animationClip, bindU1, curveU1);
                        AnimationUtility.SetEditorCurve(animationClip, bindU2, curveU2);
                        AnimationUtility.SetEditorCurve(animationClip, bindV1, curveV1);
                        AnimationUtility.SetEditorCurve(animationClip, bindV2, curveV2);
                        AnimationUtility.SetEditorCurve(animationClip, bindAspectRatio, curveRatio);
                        var settting = AnimationUtility.GetAnimationClipSettings(animationClip);
                        settting.loopTime = false;
                        AnimationUtility.SetAnimationClipSettings(animationClip, settting);
                        AssetDatabase.CreateAsset(animationClip, memeAnimDir + "Anim" + item.name+ i + ".anim");
                        var animState = stateMachineUV.AddState("Anim" + item.name + i);
                        animState.motion = animationClip;
                        animatorStates.Add(animState);
                    }
                    nameGifAnimStateMap.Add(item.name, animatorStates);


                }
                else {
                    float u1 = alatlasRects[nameArrayIndexMap[item.name]].xMin;
                    float v1 = alatlasRects[nameArrayIndexMap[item.name]].yMin;
                    float u2 = alatlasRects[nameArrayIndexMap[item.name]].xMax;
                    float v2 = alatlasRects[nameArrayIndexMap[item.name]].yMax;
                    float aspectRatio = item.keepAspectRatio ? (v2 - v1) / (u2 - u1) : 1;
                    var curveU1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, u1), new Keyframe(0.0166666666666667f, u1) });
                    var curveU2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, u2), new Keyframe(0.0166666666666667f, u2) });
                    var curveV1 = new AnimationCurve(new Keyframe[] { new Keyframe(0, v1), new Keyframe(0.0166666666666667f, v1) });
                    var curveV2 = new AnimationCurve(new Keyframe[] { new Keyframe(0, v2), new Keyframe(0.0166666666666667f, v2) });
                    var curveRatio = new AnimationCurve(new Keyframe[] { new Keyframe(0, aspectRatio), new Keyframe(0.0166666666666667f, aspectRatio) });
                    AnimationClip animationClip = new AnimationClip { name = "Anim" + item.name };
                   
                    AnimationUtility.SetEditorCurve(animationClip, bindU1, curveU1);
                    AnimationUtility.SetEditorCurve(animationClip, bindU2, curveU2);
                    AnimationUtility.SetEditorCurve(animationClip, bindV1, curveV1);
                    AnimationUtility.SetEditorCurve(animationClip, bindV2, curveV2);
                    AnimationUtility.SetEditorCurve(animationClip, bindAspectRatio, curveRatio);
                    var settting = AnimationUtility.GetAnimationClipSettings(animationClip);
                    settting.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(animationClip, settting);
                    AssetDatabase.CreateAsset(animationClip, memeAnimDir + "Anim" + item.name + ".anim");



                    var animState = stateMachineUV.AddState("Anim" + item.name);
                    animState.motion = animationClip;
                    nameAnimStateMap[item.name] = animState;


                }
               
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
            foreach (var item in nameGifAnimStateMap)
            {
                var tran4 = idleState.AddTransition(item.Value[0]);
                tran4.hasExitTime = false;
                tran4.exitTime = 0;
                tran4.hasFixedDuration = true;
                tran4.duration = 0;
                tran4.AddCondition(AnimatorConditionMode.Equals, nameIndexMap[item.Key] + 1, "MemeType_Int");

                var trans5 = idleState.AddTransition(item.Value[0]);
                trans5.hasExitTime = false;
                trans5.exitTime = 0;
                trans5.hasFixedDuration = true;
                trans5.duration = 0;
                trans5.AddCondition(AnimatorConditionMode.Equals, nameIndexMap[item.Key] + 129, "MemeType_Int");

                var trans6 = item.Value[0].AddTransition(idleState);
                trans6.hasExitTime = false;
                trans6.exitTime = 0;
                trans6.hasFixedDuration = true;
                trans6.duration = int.MaxValue;
                trans6.interruptionSource = TransitionInterruptionSource.Destination;
                trans6.AddCondition(AnimatorConditionMode.Less, 128, "MemeType_Int");
                trans6.AddCondition(AnimatorConditionMode.NotEqual, 0, "MemeType_Int");

                for (int i = 0; i < item.Value.Count - 1; i++)
                {
                    var tran7 = item.Value[i].AddTransition(item.Value[i + 1]);
                    tran7.hasExitTime = true;
                    tran7.exitTime = 1;
                    tran7.hasFixedDuration = true;
                    tran7.duration = 0;
                    tran7.interruptionSource = TransitionInterruptionSource.Source;
                    tran7.AddCondition(AnimatorConditionMode.Equals, 0, "MemeType_Int");

                    

                    var trans9 = item.Value[i].AddTransition(idleState);
                    trans9.hasExitTime = false;
                    trans9.exitTime = 0;
                    trans9.hasFixedDuration = true;
                    trans9.duration = int.MaxValue;
                    trans9.interruptionSource = TransitionInterruptionSource.Destination;
                    trans9.AddCondition(AnimatorConditionMode.Less, 128, "MemeType_Int");
                    trans9.AddCondition(AnimatorConditionMode.NotEqual, 0, "MemeType_Int");
                }
                var tran8 = item.Value[item.Value.Count - 1].AddTransition(item.Value[0]);
                tran8.hasExitTime = true;
                tran8.exitTime = 1;
                tran8.hasFixedDuration = true;
                tran8.duration = 0;
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

    }
}
