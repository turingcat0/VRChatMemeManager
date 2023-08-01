using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AnimatedValues;
using UnityEngine;
using VRCMemeManager;

namespace VRCMemeManager
{
    public class MemeInfoModel
    {


        /*
         用于UI中储存表情包信息的类
         
         */
        [System.Serializable]
        public class MemeUIInfo
        {
            public string name;
            public string type;
            public Texture2D memeTexture;
            public bool isGIF;
            public int fps;
            public bool keepAspectRatio;

            public AnimBool animBool = new AnimBool { speed = 3.0f };

            public MemeUIInfo(string _name = "新表情", string _type = "")
            {
                name = _name;
                type = _type;
            }
            public MemeUIInfo(MemeInfoData info)
            {
                name = info.name;
                type = info.type;
                memeTexture = info.memeTexture;
                isGIF = info.isGIF;
                fps = info.fps;
                keepAspectRatio = info.keepAspectRatio;
            }
        }


        /*
         用于文件中储存衣服信息的类
         
         */
        [System.Serializable]
        public class MemeInfoData
        {


            public AnimBool animBool = new AnimBool { speed = 3.0f };
            public string name; //唯一名称
            public string type; //分类
            public Texture2D memeTexture; //表情包
            public bool isGIF;
            public int fps;
            public bool keepAspectRatio;

            public MemeInfoData() { }
#if UNITY_EDITOR

            public MemeInfoData(MemeUIInfo info)
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

        /*
         用来储存所有表情包信息和参数的类
         */
        public class MenuParameter : ScriptableObject
        {
            public string avatarId; // 绑定模型ID
            public List<MemeInfoData> memeList = new List<MemeInfoData>(); // 衣服列表
        }

    }
}
