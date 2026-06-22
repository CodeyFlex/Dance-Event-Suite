using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// -----------------------------------------------------------------------
// Media Mode Toggle — PlayerData-only, mutual exclusivity, hides OHDs
// -----------------------------------------------------------------------
// Mutual exclusivity with Dancer/Staff/EventManager. When active, all OHD
// instances set canvasGroup.alpha = 0 (Media sees no numbers overhead).
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadDisplaysMedia : UdonSharpBehaviour
{
    [SerializeField]
    private Image image;

    public float MaxDistanceForClick = 5.0f;

    private Color OnColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color OffColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private Color DisabledColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

    private bool IsEnabled = false;

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            bool needsUpdate = false;
            foreach (PlayerData.Info info in infos)
            {
                if (info.Key == "Codeyflex.DanceEventSuite.MediaMode")
                {
                    IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.MediaMode");
                    needsUpdate = true;
                }
                if (info.Key == "Codeyflex.DanceEventSuite.DancerMode")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.StaffMode")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.EventManagerMode")
                    needsUpdate = true;
            }
            if (needsUpdate)
                UpdateVisual();
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.MediaMode");
            UpdateVisual();
        }
    }

    public void OnClick()
    {
        if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;

        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.DancerMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.EventManagerMode")) return;

        PlayerData.SetBool("Codeyflex.DanceEventSuite.MediaMode", !IsEnabled);
        if (!IsEnabled)
        {
            PlayerData.SetBool("Codeyflex.DanceEventSuite.DancerMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.StaffMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.EventManagerMode", false);
        }
    }

    private void UpdateVisual()
    {
        bool dancerEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.DancerMode");
        bool staffEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode");
        bool eventManagerEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.EventManagerMode");
        if (dancerEnabled || staffEnabled || eventManagerEnabled)
            image.color = DisabledColor;
        else
            image.color = IsEnabled ? OnColor : OffColor;
    }
}