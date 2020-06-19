using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;



namespace ManeuverNodeSplitter
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings
    // HighLogic.CurrentGame.Parameters.CustomParams<MNS>().

    public class MNS : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Maneuver Node Splitter"; } } // Column header
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Maneuver Node Splitter"; } }
        public override string DisplaySection { get { return "Maneuver Node Splitter"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomIntParameterUI("Split by AP Iterations", minValue = 1, maxValue = 99)]
        public int splitByApoIterations = 18;

        [GameParameters.CustomIntParameterUI("Split by Burn Time Iterations", minValue = 1, maxValue = 99)]
        public int splitByBurnTimeIterations = 18;

        [GameParameters.CustomIntParameterUI("Split by Period Iterations", minValue = 1, maxValue = 99)]
        public int splitByPeriodIterations = 18;

        [GameParameters.CustomParameterUI("Alternate skin")]
        public bool altSkin = true;

        [GameParameters.CustomParameterUI("Default mode: by dV")]
        public bool byDv = true;

        [GameParameters.CustomParameterUI("Default mode: by Period")]
        public bool byPeriod = false;

        [GameParameters.CustomParameterUI("Default mode: by Apopsis")]
        public bool byAp = false;

        [GameParameters.CustomParameterUI("Default mode: by Burn Time")]
        public bool byBurnTime = false;

        bool oldByDv, oldByPeriod, oldByAp, oldBurnTime;
        bool initted = false;


        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (!initted)
            {
                oldByDv = byDv;
                oldByPeriod = byPeriod;
                oldByAp = byAp;
                oldBurnTime = byBurnTime;
                initted = true;
            }
            if (byDv && !oldByDv)
            {
                oldByDv = true;
                oldByPeriod = byPeriod = false;
                oldByAp = byAp = false;
                oldBurnTime = byBurnTime = false;
            }
            if (byPeriod && !oldByPeriod)
            {
                oldByPeriod = true;
                oldByDv = byDv = false;
                oldByAp = byAp = false;
                oldBurnTime = byBurnTime = false;
            }
            if (byAp && !oldByAp)
            {
                oldByAp = true;
                oldByPeriod = byPeriod = false;
                oldByDv = byDv = false;
                oldBurnTime = byBurnTime = false;
            }
            if (byBurnTime && !oldBurnTime)
            {
                oldBurnTime = true;
                oldByPeriod = byPeriod = false;
                oldByAp = byAp = false;
                oldByDv = byDv = false;
            }
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters) { return true; }

        public override IList ValidValues(MemberInfo member) { return null; }

    }
}
