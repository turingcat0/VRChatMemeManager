using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VRCMemeManager.MemeInfoModel;
using UnityEngine;

namespace VRCMemeManager
{
    /*
       用来储存所有表情包信息和参数的类

     */
    public class MenuParameter : ScriptableObject
    {
        public string avatarId; // 绑定模型ID
        public string menuPath;
        public string menuName;
        public List<MemeInfoData> memeList = new List<MemeInfoData>(); // 衣服列表
    }
}
