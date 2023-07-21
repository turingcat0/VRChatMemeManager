using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace VRCMemeManager
{
    public class MemeManagerParameter : ScriptableObject
    {
        internal string avatarId; // 绑定模型ID
        public List<MemeInfo> memeList = new List<MemeInfo>(); // 衣服列表
        //test

        [System.Serializable]
        public class MemeInfo
        {
            public AnimBool animBool = new AnimBool { speed = 3.0f };
            public string name; //唯一名称
            public string type; //分类
            public Texture2D memeTexture; //表情包
            public bool isGIF;
            public int fps;
            public bool keepAspectRatio;

            public MemeInfo() { }
#if UNITY_EDITOR

            public MemeInfo(MemeManagerUtils.MemeItemInfo info)
            {
                name = info.name;
                type = info.type;
                memeTexture = info.memeTexture;
                isGIF = info.isGIF;
                fps = info.fps;
                keepAspectRatio = info.keepAspectRatio;
            }
#endif
        }
    }
}
