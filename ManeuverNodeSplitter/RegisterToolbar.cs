using UnityEngine;
using ToolbarControl_NS;

namespace ManeuverNodeSplitter
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(NodeSplitterAddon.MODID, NodeSplitterAddon.MODNAME);
        }
    }
}
