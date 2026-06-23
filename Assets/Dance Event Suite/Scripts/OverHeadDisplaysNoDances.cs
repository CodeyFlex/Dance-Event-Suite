using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// -----------------------------------------------------------------------
// NoDances Toggle — independent, no mutual exclusivity
// -----------------------------------------------------------------------
// Anyone can toggle regardless of current role. Blocks most remote clicks
// on OHDs (EventManager bypasses by design).
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadDisplaysNoDances : UdonSharpBehaviour
{
    [SerializeField]
    private Image image;

    public float MaxDistanceForClick = 5.0f;

    [SerializeField] private Color OnColor  = new Color(0.0f, 1.0f, 0.0f, 1.0f); // Green
    [SerializeField] private Color OffColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red

    private bool IsEnabled = false;

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            foreach (PlayerData.Info info in infos)
            {
                if (info.Key == "Codeyflex.DanceEventSuite.NoDances")
                {
                    IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.NoDances");
                    if (image != null)
                        image.color = IsEnabled ? OnColor : OffColor;
                }
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.NoDances");
            if (image != null)
                image.color = IsEnabled ? OnColor : OffColor;
        }
    }

    public void OnClick()
    {
        if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;
        PlayerData.SetBool("Codeyflex.DanceEventSuite.NoDances", !IsEnabled);
    }
}
