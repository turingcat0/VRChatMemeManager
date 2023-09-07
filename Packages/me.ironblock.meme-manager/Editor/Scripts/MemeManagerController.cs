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
using static VRCMemeManager.MemeInfoModel;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

namespace VRCMemeManager
{
    public class MemeManagerController
    {
    // 创建表情包参数文件
    internal static MenuParameter CreateMemeManagerParameter(GameObject avatar)
        {
            if (avatar == null)
                return null;
            var parameter = ScriptableObject.CreateInstance<MenuParameter>();
            var avatarId = Utils.GetOrCreateAvatarId(avatar);
            parameter.avatarId = avatarId;
            parameter.memeList.Add(new MemeInfoData { name = "表情包1", type = "" });
            var dir = Utils.GetParameterDirPath(avatarId, "");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(parameter, Utils.GetParameterDirPath(avatarId, "MemeManagerParameter.asset"));
            return parameter;
        }
       
        // 通过检测type字段，判断是否为分类模式
        internal static bool HasClassify(List<MemeInfoData> list)
        {
            foreach (var item in list)
                if (item.type != null && item.type.Length > 0)
                    return true;
            return false;
        }
        // 通过检测type字段，判断是否为分类模式
        internal static bool HasClassify(List<MemeUIInfo> list)
        {
            foreach (var item in list)
                if (item.type != null && item.type.Length > 0)
                    return true;
            return false;
        }

        internal static VRCExpressionsMenu SearchOrCreateSubMenu(string menuAssetDir,VRCExpressionsMenu root, string[] paths, int currentIndex)
        {
            if (paths.Length == currentIndex)
            {
                return root;
            }
            // 表情包
            foreach (var control in root.controls)
            {
                if (control.name.Equals(paths[currentIndex]))
                {

                    if (control.type != VRCExpressionsMenu.Control.ControlType.SubMenu)
                    {
                        EditorUtility.DisplayDialog("提醒", "您所输入的菜单路径不是子菜单", "确认");
                        return null;
                    }
                    else
                    {
                        return SearchOrCreateSubMenu(menuAssetDir,control.subMenu, paths, currentIndex + 1);
                    }
                }
            }

            var subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(subMenu, menuAssetDir + paths[currentIndex] + ".asset");
            root.controls.Add(new VRCExpressionsMenu.Control
            {
                name = paths[currentIndex],
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu,
            });
            EditorUtility.SetDirty(root);
            return SearchOrCreateSubMenu(menuAssetDir, subMenu, paths, currentIndex + 1);
        }

        // 应用到模型
        internal static void ApplyToAvatar(GameObject avatar, MenuParameter parameter)
        {
            var memeList = new List<MemeInfoData>();
            var _memeList = parameter.memeList;
            foreach (var info in _memeList)
            {
                if (info.memeTexture == null) continue;
                memeList.Add(info);
            }

            var avatarId = Utils.GetAvatarId(avatar);
            var dirPath = Utils.GetParameterDirPath(avatarId, "");
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
            var shader = Resources.Load<Shader>("Materials/MemeEmitterShader");
            var shaderGif = Resources.Load<Shader>("Materials/MemeEmitterShaderGif");
            Dictionary<string, Material> NameMaterialMap = new Dictionary<string, Material>();
            Dictionary<string, int> nameLengthMap = new Dictionary<string, int>();
            foreach (var item in memeList)
            {
                Texture tex;
                Material material;
                if (item.isGIF)
                {
                    material = new Material(shaderGif);
                    var t2da = Utils.GifToTextureArray(AssetDatabase.GetAssetPath(item.memeTexture));
                    nameLengthMap.Add(item.name, t2da.depth);
                    tex = t2da;
                    AssetDatabase.CreateAsset(tex, textureDir + item.name.GetHashCode() + ".asset");
                }
                else {
                    material = new Material(shader);
                    tex = item.memeTexture;
                }
                 
                NameMaterialMap.Add(item.name, material);
                
                material.mainTexture = tex;

                AssetDatabase.CreateAsset(material, textureDir + item.name.GetHashCode() + ".mat");
                
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
           
            



            AnimationClip disableMemeEmitterAnim = Resources.Load<AnimationClip>("Animations/DisableMemeEmitter");
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

            Utils.AddControllerParameter(fxController, "MemeType_Int", AnimatorControllerParameterType.Int);
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
          
          
            //其余的
            EditorCurveBinding bindTexture = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "m_Materials.Array.data[0]",
                type = typeof(ParticleSystemRenderer)
            };
          
            int j = 0;
            AnimationCurve curve = new AnimationCurve(new Keyframe[] {
                    new Keyframe(0, 8),
                    new Keyframe(0.5f, 8),
                    new Keyframe(0.5f, 0),
                });
            EditorCurveBinding bindEnable = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "EmissionModule.rateOverTime.scalar",
                type = typeof(ParticleSystem)
            };

            EditorCurveBinding bindLength = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "material._Length",
                type = typeof(ParticleSystemRenderer)
            };
            EditorCurveBinding bindFPS = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "material._FPS",
                type = typeof(ParticleSystemRenderer)
            };
            EditorCurveBinding bindAspectRatio = new EditorCurveBinding
            {
                path = "MemeEmitter",
                propertyName = "material._AspectRatio",
                type = typeof(ParticleSystemRenderer)
            };


            foreach (var item in memeList)
            {
                float aspectRatio = item.keepAspectRatio ? ((float)item.memeTexture.height/item.memeTexture.width) : 1;
                var curveRatio = new AnimationCurve(new Keyframe[] { new Keyframe(0, aspectRatio), new Keyframe(0.0166666666666667f, aspectRatio) });
                ObjectReferenceKeyframe[] objectReferenceKeyframes = new ObjectReferenceKeyframe[]
                    { 
                        new ObjectReferenceKeyframe{ time = 0, value = NameMaterialMap[item.name]},
                        new ObjectReferenceKeyframe{ time = 0.5f, value = NameMaterialMap[item.name]}
                    };
                var animClip = new AnimationClip();
                AnimationUtility.SetObjectReferenceCurve(animClip, bindTexture, objectReferenceKeyframes);
                AnimationUtility.SetEditorCurve(animClip, bindEnable, curve);
                AnimationUtility.SetEditorCurve(animClip, bindAspectRatio, curveRatio);
                if (item.isGIF)
                {
                    var fpsCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, item.fps), new Keyframe(0.0166666666666667f, item.fps) });
                    AnimationUtility.SetEditorCurve(animClip, bindFPS, fpsCurve);
                    int length = nameLengthMap[item.name];
                    var lengthCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, length), new Keyframe(0.0166666666666667f, length) });
                    AnimationUtility.SetEditorCurve(animClip, bindLength, lengthCurve);
                }

                AssetDatabase.CreateAsset(animClip, memeAnimDir + item.name.GetHashCode() + ".asset");

                var stateTmp = stateMachineParameters.AddState(item.name);
                stateTmp.motion = animClip;
                
                var trans8 = disableMemeEmitterState.AddTransition(stateTmp);
                trans8.exitTime = 0;
                trans8.hasExitTime = false;
                trans8.duration = 0;
                trans8.hasFixedDuration = false;
                trans8.AddCondition(AnimatorConditionMode.Equals, j + 1, "MemeType_Int");

                var trans9 = stateTmp.AddTransition(disableMemeEmitterState);
                trans9.hasExitTime = true;
                trans9.exitTime = 1;
                trans9.hasFixedDuration = true;
                trans9.duration = float.MaxValue;
                trans9.interruptionSource = TransitionInterruptionSource.Destination;
             
                j++;
            }
            var parameterDriver = disableMemeEmitterState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            parameterDriver.parameters.Add(new VRC_AvatarParameterDriver.Parameter() { type = VRC_AvatarParameterDriver.ChangeType.Set, name = "MemeType_Int", value = 0 });


            fxController.AddLayer(new AnimatorControllerLayer
            {
                name = stateMachineParameters.name,
                defaultWeight = 1f,
                stateMachine = stateMachineParameters
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
                    var memeTypeMap = new Dictionary<string, List<MemeInfoData>>();
                    foreach (var info in memeList)
                    {
                        var type = (info.type.Length == 0 ? "未分类" : info.type);
                        if (!memeTypeMap.ContainsKey(type))
                            memeTypeMap.Add(type, new List<MemeInfoData>());
                        memeTypeMap[type].Add(info);
                    }

                    var typeNameGeneratedCountMap = new Dictionary<string, int>();
                    // 生成类型菜单
                    foreach (var item in memeTypeMap)
                    {
                        var name = item.Key;
                        var infoList = item.Value;

                        var menuList = new List<VRCExpressionsMenu>();
                        var nowMemeMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                        EditorUtility.SetDirty(nowMemeMenu);
                        menuList.Add(nowMemeMenu);

                        
                        // 判断是否已分类
                        if (!hasClassify)
                            mainMemeMenu = nowMemeMenu;
                       

                        //考虑到正常人不会做8个以上的分类, 所以就不添加下一页检测了
                        if (hasClassify)
                            mainMemeMenu.controls.Add(new VRCExpressionsMenu.Control
                            {
                                name = name,
                                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = nowMemeMenu
                            });
                        if (!typeNameGeneratedCountMap.ContainsKey(item.Key))
                        {
                            typeNameGeneratedCountMap.Add(item.Key, 0);
                        }

                        

                        foreach (var info in infoList)
                        {
                            if (nowMemeMenu.controls.Count == 7&&(item.Value.Count - typeNameGeneratedCountMap[item.Key]) != 1)
                            {
                                var newMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
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
                            EditorUtility.SetDirty(nowMemeMenu);
                            typeNameGeneratedCountMap[item.Key]++;
                        }
                        int index = 0;
                        foreach (var item1 in menuList)
                        {
                            AssetDatabase.CreateAsset(item1, menuDir + "MemeType_" + name.GetHashCode() + "_" + (index) + ".asset");
                            index++;
                        }

                    }

                    if (hasClassify)
                        AssetDatabase.CreateAsset(mainMemeMenu, menuDir + "ActionMenu.asset");
                }

                // 配置主菜单
                var menuPath = parameter.menuPath;
                var menuName = parameter.menuName;

                if (expressionsMenu == null)
                    expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                if (menuPath.Equals(""))
                {
                    expressionsMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = menuName,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = mainMemeMenu,
                    });
                }
                else 
                {
                    var paths = menuPath.Split('/');
                    var finalMenu = SearchOrCreateSubMenu(menuDir, expressionsMenu, paths, 0);
                    if (finalMenu == null)
                    {
                        return;
                    }
                    finalMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = menuName,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = mainMemeMenu,
                    });
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            descriptor.customExpressions = true;
            descriptor.expressionParameters = expressionParameters;
            descriptor.expressionsMenu = expressionsMenu;
            EditorUtility.DisplayDialog("提醒", "应用成功，快上传模型测试下效果吧~", "确认");
        }

    }
}
