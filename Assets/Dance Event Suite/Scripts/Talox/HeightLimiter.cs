
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class HeightLimiter : UdonSharpBehaviour
{
    [SerializeField]
    private Image image;
    [SerializeField]
    private TMP_Text text;
    
    public float MaxHeight = 2;
    public float MinHeight = 1.6f;
    
    private Color OnColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color OffColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private string OnMessage = "Height Limit: On";
    private string OffMessage = "Height Limit: Off";
    
    private bool IsEnabled = false;
    
    public void Start()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        player.SetAvatarEyeHeightMaximumByMeters(MaxHeight);
        player.SetAvatarEyeHeightMinimumByMeters(MinHeight);
        image.color = OnColor;
        text.text = OnMessage;
    }
    
    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (player.isLocal)
        {
            foreach (PlayerData.Info info in infos)
            {
                if (info.Key == "Talox.DancerGuidance.HeightLimit")
                {
                    IsEnabled = PlayerData.GetBool(player, "Talox.DancerGuidance.HeightLimit");
                    
                    if(IsEnabled)
                    {
                        player.SetAvatarEyeHeightMaximumByMeters(100);
                        player.SetAvatarEyeHeightMinimumByMeters(0);
                        image.color = OffColor;
                        text.text = OffMessage;
                    }
                    else
                    {
                        player.SetAvatarEyeHeightMaximumByMeters(MaxHeight);
                        player.SetAvatarEyeHeightMinimumByMeters(MinHeight);
                        player.SetAvatarEyeHeightByMeters(Mathf.Clamp(player.GetAvatarEyeHeightAsMeters(), MinHeight, MaxHeight));
                        image.color = OnColor;
                        text.text = OnMessage;
                    }
                }
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if(player.isLocal)
        {
            if (!PlayerData.HasKey(player, "Talox.DancerGuidance.HeightLimit"))
            {
                PlayerData.SetBool("Talox.DancerGuidance.HeightLimit", false);
                player.SetAvatarEyeHeightMaximumByMeters(MaxHeight);
                player.SetAvatarEyeHeightMinimumByMeters(MinHeight);
                player.SetAvatarEyeHeightByMeters(Mathf.Clamp(player.GetAvatarEyeHeightAsMeters(), MinHeight, MaxHeight));
                image.color = OnColor;
                text.text = OnMessage;
            }
            else
            {
                IsEnabled = PlayerData.GetBool(player, "Talox.DancerGuidance.HeightLimit");
                
                if(IsEnabled)
                {
                    player.SetAvatarEyeHeightMaximumByMeters(100);
                    player.SetAvatarEyeHeightMinimumByMeters(0);
                    image.color = OffColor;
                    text.text = OffMessage;
                }
                else
                {
                    player.SetAvatarEyeHeightMaximumByMeters(MaxHeight);
                    player.SetAvatarEyeHeightMinimumByMeters(MinHeight);
                    player.SetAvatarEyeHeightByMeters(Mathf.Clamp(player.GetAvatarEyeHeightAsMeters(), MinHeight, MaxHeight));
                    image.color = OnColor;
                    text.text = OnMessage;
                }
            }
        }
    }

    public void OnClick()
    {
        PlayerData.SetBool("Talox.DancerGuidance.HeightLimit", !IsEnabled);
    }
}
