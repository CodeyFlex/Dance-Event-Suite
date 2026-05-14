
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadNumberEnabled : UdonSharpBehaviour
{
    [SerializeField]
    private Image image;

    private Color OnColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color OffColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

    private bool IsEnabled = false;

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            foreach (PlayerData.Info info in infos)
            {
                if (info.Key == "Talox.DancerGuidance.OverHeadNumber")
                {
                    IsEnabled = PlayerData.GetBool(player, "Talox.DancerGuidance.OverHeadNumber");
                    image.color = IsEnabled ? OnColor : OffColor;
                }
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            IsEnabled = PlayerData.GetBool(player, "Talox.DancerGuidance.OverHeadNumber");
            image.color = IsEnabled ? OnColor : OffColor;
        }
    }

    public void OnClick()
    {
        PlayerData.SetBool("Talox.DancerGuidance.OverHeadNumber", !IsEnabled);
    }
}
