using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;


using ToolbarControl_NS;
using ClickThroughFix;
using SpaceTuxUtility;

namespace ManeuverNodeSplitter
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class NodeSplitterAddon : MonoBehaviour, INodeSplitterAddon
    {
        protected PatchedConicSolver Solver { get { return FlightGlobals.ActiveVessel == null ? null : FlightGlobals.ActiveVessel.patchedConicSolver; } }

        ToolbarControl toolbarControl;

        private bool visible;
        private int windowId;
        private Rect position = new Rect(100, 300, 200, 100);

        private List<INodeSplitter> splitters = new List<INodeSplitter>();
        private INodeSplitter currentSplitter;

        protected List<Maneuver> oldManeuvers = new List<Maneuver>();
        WindowHelper windowHelper;
        public void Awake()
        {
            windowHelper = new WindowHelper();
            windowId = WindowHelper.NextWindowId("NodeSplitter");

            splitters.Add(new NodeSplitterByApoapsis(this));
            if (NodeSplitterByBurnTime.Available)
            {
                splitters.Add(new NodeSplitterByBurnTime(this));
            }
            splitters.Add(new NodeSplitterByDeltaV(this));
            splitters.Add(new NodeSplitterByPeriod(this));
            splitters.Sort((l, r) => { return string.Compare(l.GetName(), r.GetName(), true); });
            currentSplitter = splitters[0];
        }
        internal const string MODID = "ManeuverNodeSplitter_NS";
        internal const string MODNAME = "Maneuver Node Splitter";

        bool hideUI;

        public void Start()
        {
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);

            //if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(ToggleVisibility, ToggleVisibility,
                    ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT,
                    MODID,
                    "nodeSplitterButton",
                    "NodeSplitter/PluginData/ejection38",
                    "NodeSplitter/PluginData/ejection24",
                    MODNAME
                );
            }

        }

        public void OnDestroy()
        {
            toolbarControl.OnDestroy();
            Destroy(toolbarControl);

            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);

        }

        public void ResetWindow()
        {
            position.height = 100;
        }

        private void ToggleVisibility()
        {
            visible = !visible;
        }

        private bool initialized = false;
        private void OnFirstGUI()
        {
            string defaultMode = "";
            if (HighLogic.CurrentGame.Parameters.CustomParams<MNS>().byDv)
                defaultMode = "by dV";
            if (HighLogic.CurrentGame.Parameters.CustomParams<MNS>().byAp)
                defaultMode = "by Apoapsis";
            if (HighLogic.CurrentGame.Parameters.CustomParams<MNS>().byBurnTime)
                defaultMode = "by Burn Time";
            if (HighLogic.CurrentGame.Parameters.CustomParams<MNS>().byPeriod)
                defaultMode = "by Period";

            int index = splitters.FindIndex((s) => { return string.Compare(s.GetName(), defaultMode, true) == 0; });
            if (index >= 0)
            {
                currentSplitter = splitters[index];
            }
            initialized = true;
        }
        private void ShowUI()
        {
            hideUI = false;
        }
        void HideUI()
        {
            hideUI = true;
        }



        internal void OnGUI()
        {
            if (!initialized)
            {
                OnFirstGUI();
            }
            if (!hideUI && visible && currentSplitter != null)
            {
                if (!HighLogic.CurrentGame.Parameters.CustomParams<MNS>().altSkin)
                    GUI.skin = HighLogic.Skin;

                position = ClickThruBlocker.GUILayoutWindow(windowId, position, currentSplitter.DrawWindow, "Node Splitter", GUILayout.ExpandHeight(true));
            }
        }

        public void DrawHeader()
        {
            GUILayout.BeginHorizontal();

            GUIStyle alignCenter = new GUIStyle(GUI.skin.label);
            alignCenter.alignment = TextAnchor.MiddleCenter;

            if (GUILayout.Button("<", GUILayout.Width(30)))
                PreviousSplitter();
            GUILayout.Label(currentSplitter.GetName(), alignCenter, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(30)))
                NextSplitter();

            GUILayout.EndHorizontal();
        }

        public void DrawFooter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.ExpandWidth(true));
            if (oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                if (GUILayout.Button("Undo", GUILayout.Width(70)))
                {
                    UndoSplit();
                    ResetWindow();
                }
            }
            else
            {
                GUILayout.Label(" ", GUILayout.Width(70));
            }
            if (GUILayout.Button("Apply", GUILayout.Width(70)))
            {
                currentSplitter.SplitNode();
            }
            GUILayout.EndHorizontal();
        }

        internal void PreviousSplitter()
        {
            int index = splitters.IndexOf(currentSplitter) - 1;
            if (index < 0)
            {
                index = splitters.Count - 1;
            }
            currentSplitter = splitters[index];
            ResetWindow();
        }

        internal void NextSplitter()
        {
            int index = splitters.IndexOf(currentSplitter) + 1;
            if (index >= splitters.Count)
            {
                index = 0;
            }
            currentSplitter = splitters[index];
            ResetWindow();
        }

        protected bool IsSolverAvailable()
        {
            return HighLogic.LoadedSceneIsFlight && Solver != null;
        }

        public List<Maneuver> SaveManeuvers()
        {
            List<Maneuver> oldSaves = new List<Maneuver>(oldManeuvers);
            oldManeuvers.Clear();
            foreach (ManeuverNode node in Solver.maneuverNodes)
            {
                oldManeuvers.Add(new Maneuver(node));
            }
            return oldSaves;
        }

        private void UndoSplit()
        {
            if (IsSolverAvailable() && oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                List<Maneuver> toRestore = SaveManeuvers();

                while (Solver.maneuverNodes.Count > 0)
                {
                    Solver.maneuverNodes[0].RemoveSelf();
                }
                foreach (Maneuver m in toRestore)
                {
                    ManeuverNode node = Solver.AddManeuverNode(m.UT);
                    node.DeltaV = m.DeltaV;
                    node.solver.UpdateFlightPlan();
                }

                ScreenMessages.PostScreenMessage(string.Format("Replaced flight plan with {0} saved maneuvers.", toRestore.Count), 8f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
    }
}
