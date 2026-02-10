using System;
using System.Collections.Generic;
using System.Text;
using RoR2.Skills;
using RoR2;
using UnityEngine;
using static RoR2.Skills.SkillFamily;
using R2API.Utils;

namespace NewGenSkillSwapper
{

    public class ExtraSkillSlotsCompat
    {
        public static bool IsEnabled()
        {
            return false;
            //return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ExtraSkillSlotsPlugin.GUID);
        }
    }
}
