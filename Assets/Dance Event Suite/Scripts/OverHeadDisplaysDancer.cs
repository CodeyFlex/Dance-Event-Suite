using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// -----------------------------------------------------------------------
// Dancer Mode Toggle — PlayerData-only (BehaviourSyncMode.None)
// -----------------------------------------------------------------------
// Mutual exclusivity: turning Dancer ON forces Staff/Media/EventManager OFF.
// Greyed out (DisabledColor) whenever another role is active. All state
// lives in PlayerData — no synced variables needed.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadDisplaysDancer : UdonSharpBehaviour
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
                if (info.Key == "Codeyflex.DanceEventSuite.DancerMode")
                {
                    IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.DancerMode");
                    needsUpdate = true;
                }
                if (info.Key == "Codeyflex.DanceEventSuite.StaffMode")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.MediaMode")
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
            IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.DancerMode");
            UpdateVisual();
        }
    }

    public void OnClick()
    {
        if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;
        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.MediaMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.EventManagerMode")) return;

        PlayerData.SetBool("Codeyflex.DanceEventSuite.DancerMode", !IsEnabled);
        if (!IsEnabled)
        {
            PlayerData.SetBool("Codeyflex.DanceEventSuite.StaffMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.MediaMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.EventManagerMode", false);
        }
    }

    private void UpdateVisual()
    {
        if (image == null) return;
        bool staffEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode");
        bool mediaEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.MediaMode");
        bool eventManagerEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.EventManagerMode");
        if (staffEnabled || mediaEnabled || eventManagerEnabled)
            image.color = DisabledColor;
        else
            image.color = IsEnabled ? OnColor : OffColor;
    }
}
