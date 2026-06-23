using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

// -----------------------------------------------------------------------
// Event Manager Mode Toggle — PlayerData-only, fourth role
// -----------------------------------------------------------------------
// Mutual exclusivity with Dancer/Staff/Media.
// When active, OHD clicks (local + remote) cycle audienceSpecialState
// (Birthday!/Freshie!) instead of incrementing numbers. BehaviourSyncMode.None
// — all state lives in PlayerData.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadDisplaysEventManager : UdonSharpBehaviour
{
    [SerializeField]
    private Image image;

    public float MaxDistanceForClick = 5.0f;

    [SerializeField] private Color _selectedColor   = new Color(0.0f, 0.9f, 0.0f, 1.0f); // Green
    [SerializeField] private Color _unselectedColor = new Color(0.9f, 0.0f, 0.0f, 1.0f); // Red
    [SerializeField] private Color _lockedColor     = new Color(0.35f, 0.35f, 0.35f, 1.0f); // Gray

    private bool IsEnabled = false;

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            bool needsUpdate = false;
            foreach (PlayerData.Info info in infos)
            {
                if (info.Key == "Codeyflex.DanceEventSuite.EventManagerMode")
                {
                    IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.EventManagerMode");
                    needsUpdate = true;
                }
                if (info.Key == "Codeyflex.DanceEventSuite.DancerMode")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.StaffMode")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.MediaMode")
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
            IsEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.EventManagerMode");
            UpdateVisual();
        }
    }

    public void OnClick()
    {
        if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;

        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.DancerMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.MediaMode")) return;

        PlayerData.SetBool("Codeyflex.DanceEventSuite.EventManagerMode", !IsEnabled);
        if (!IsEnabled)
        {
            PlayerData.SetBool("Codeyflex.DanceEventSuite.DancerMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.StaffMode", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.MediaMode", false);
        }
    }

    private void UpdateVisual()
    {
        if (image == null) return;
        bool dancerEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.DancerMode");
        bool staffEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode");
        bool mediaEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.MediaMode");
        if (dancerEnabled || staffEnabled || mediaEnabled)
            image.color = _lockedColor;
        else
            image.color = IsEnabled ? _selectedColor : _unselectedColor;
    }
}
