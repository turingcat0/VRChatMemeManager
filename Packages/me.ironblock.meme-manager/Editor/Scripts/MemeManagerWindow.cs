﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRCMemeManager
{
    public class MemeManagerWindow : EditorWindow
    {
        internal const int maxMemeNum = 127;

        private Vector2 mainScrollPos;

        private GameObject avatar;
        private MemeManagerParameter parameter;
        private string avatarId;
        private List<MemeManagerUtils.MemeItemInfo> memeItemList = new List<MemeManagerUtils.MemeItemInfo>();


        private void OnEnable()
        {
            foreach (var info in memeItemList)
            {
                info.animBool.valueChanged.RemoveAllListeners();
                info.animBool.valueChanged.AddListener(Repaint);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUI.skin.label.fontSize = 24;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("表情包管理器");
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("by:Iron__Block");
            GUILayout.Space(10);
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("轻松管理表情包");
            GUILayout.Space(10);

            var newAvatar = (GameObject)EditorGUILayout.ObjectField("选择模型：", avatar, typeof(GameObject), true);
            if (avatar != newAvatar)
            {
                avatar = newAvatar;
                if (newAvatar != null && newAvatar.GetComponent<VRCAvatarDescriptor>() == null)
                {
                    avatar = null;
                    EditorUtility.DisplayDialog("提醒", "本插件仅供SDK3模型使用！", "确认");
                }
                parameter = null;
                memeItemList.Clear();
                if (avatar != null)
                {
                    avatarId = MoyuToolkitUtils.GetOrCreateAvatarId(avatar);
                    ReadParameter();
                }
            }
            GUILayout.Space(10);
            if (avatar == null)
            {
                EditorGUILayout.HelpBox("请先选择一个模型", MessageType.Info);
                GUILayout.Space(10);
            }
            else if (parameter == null)
            {
                MoyuToolkitUtils.Print("创建了新的配置文件");
                parameter = MemeManagerUtils.CreateMemeManagerParameter(avatar);
                ReadParameter();
            }
            else
            {
                mainScrollPos = GUILayout.BeginScrollView(mainScrollPos);
                // 主UI
                EditorGUI.BeginChangeCheck();
                var sum = parameter.memeList.Count;
                if (sum == 0)
                {
                    EditorGUILayout.HelpBox("当前表情包列表为空，先点击下面按钮添加一个吧", MessageType.Info);
                }
                else
                {
                    var memeNameList = new List<string>();
                    foreach (var info in memeItemList)
                        memeNameList.Add(info.name);
                    // 遍历信息
                    EditorGUILayout.LabelField("动作列表：");
                    var classify = MemeManagerUtils.HasClassify(memeItemList);
                    for (var index = 0; index < sum; index++)
                    {
                        var info = memeItemList[index];
                        var name = (classify ? "【" + (info.type.Length > 0 ? info.type : "未分类") + "】" : "") + info.name;
                        var newTarget = EditorGUILayout.Foldout(info.animBool.target, name, true);
                        if (newTarget != info.animBool.target)
                        {
                            if (newTarget)
                                foreach (var _info in memeItemList)
                                    _info.animBool.target = false;
                            info.animBool.target = newTarget;
                        }
                        if (EditorGUILayout.BeginFadeGroup(info.animBool.faded))
                        {
                            // 样式嵌套Start
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            GUILayout.Space(5);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(15);
                            EditorGUILayout.BeginVertical();

                            // 内容
                            EditorGUILayout.BeginHorizontal();
                            info.memeTexture = (Texture2D)EditorGUILayout.ObjectField("", info.memeTexture, typeof(Texture2D), true, GUILayout.Width(60), GUILayout.Height(60));
                            GUILayout.Space(5);
                            EditorGUILayout.BeginVertical();

                            //操作按钮
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (index > 0 && GUILayout.Button("上移", GUILayout.Width(60)))
                            {
                                MoyuToolkitUtils.MoveListItem(ref memeItemList, index, index - 1);
                                break;
                            }
                            else if (index < memeItemList.Count - 1 && GUILayout.Button("下移", GUILayout.Width(60)))
                            {
                                MoyuToolkitUtils.MoveListItem(ref memeItemList, index, index + 1);
                                break;
                            }
                            if (GUILayout.Button("删除", GUILayout.Width(60)))
                            {
                                RemoveMeme(index);
                                break;
                            }
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();
                            //唯一动作名
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.LabelField("动作名称", GUILayout.Width(55));
                            var newName = EditorGUILayout.TextField(info.name).Trim();
                            if (!memeNameList.Contains(newName) && newName.Length > 0)
                                info.name = newName;
                            EditorGUILayout.EndVertical();
                            //分类
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.LabelField("分类", GUILayout.Width(55));
                            info.type = EditorGUILayout.TextField(info.type).Trim();
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();


                            // 样式嵌套End
                            EditorGUILayout.EndVertical();
                            GUILayout.Space(5);
                            EditorGUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                    // 检测是否有修改
                    if (EditorGUI.EndChangeCheck())
                    {
                        WriteParameter();
                    }
                }
                GUILayout.EndScrollView();
                if (sum < maxMemeNum && GUILayout.Button("添加表情包"))
                    AddMeme();

                GUILayout.Space(5);

                //下操作栏
                GUILayout.Space(10);

                GUILayout.Label("操作菜单");
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("一键应用到模型"))
                    MemeManagerUtils.ApplyToAvatar(avatar, parameter);

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

        }
        private void AddMeme()
        {
            foreach (var info in memeItemList)
                info.animBool.target = false;
            var name = "表情包" + (memeItemList.Count + 1).ToString();
            var actionItemInfo = new MemeManagerUtils.MemeItemInfo(name);
            actionItemInfo.animBool.valueChanged.AddListener(Repaint);
            actionItemInfo.animBool.target = true;
            memeItemList.Add(actionItemInfo);
            WriteParameter();
        }
        private void RemoveMeme(int index)
        {
            if (!EditorUtility.DisplayDialog("注意", "真的要删除这个表情包吗？", "确认", "取消"))
                return;
            memeItemList.RemoveAt(index);
            WriteParameter();
        }
        private void ReadParameter()
        {
            memeItemList.Clear();
            if (avatarId == null) return;
            if (parameter == null) parameter = MemeManagerUtils.GetMemeManagerParameter(avatarId);
            if (parameter == null) return;
            foreach (var info in parameter.memeList)
            {
                var item = new MemeManagerUtils.MemeItemInfo(info);
                item.animBool.valueChanged.AddListener(Repaint);
                memeItemList.Add(item);
            }
        }
        private void WriteParameter()
        {
            if (parameter == null) return;
            var actionList = new List<MemeManagerParameter.MemeInfo>();
            foreach (var info in memeItemList)
            {
                var item = new MemeManagerParameter.MemeInfo(info);
                actionList.Add(item);
            }
            parameter.memeList = actionList;
            EditorUtility.SetDirty(parameter);
        }

    }
}
