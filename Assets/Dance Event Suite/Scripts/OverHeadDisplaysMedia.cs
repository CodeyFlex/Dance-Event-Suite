using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

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
                if (info.Key == "Codeyflex.DanceEventSuite.OverHeadDisplays")
                    needsUpdate = true;
                if (info.Key == "Codeyflex.DanceEventSuite.StaffMode")
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

        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.OverHeadDisplays") ||
            PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode")) return;

        PlayerData.SetBool("Codeyflex.DanceEventSuite.MediaMode", !IsEnabled);
        if (!IsEnabled)
        {
            PlayerData.SetBool("Codeyflex.DanceEventSuite.OverHeadDisplays", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.StaffMode", false);
        }
    }

    private void UpdateVisual()
    {
        bool dancerEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.OverHeadDisplays");
        bool staffEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode");
        if (dancerEnabled || staffEnabled)
            image.color = DisabledColor;
        else
            image.color = IsEnabled ? OnColor : OffColor;
    }
}