
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class JoinButton : UdonSharpBehaviour
{
    private bool isLocked = false;
    
    [SerializeField]
    private LeaderboardObject leaderboardObject;
    [SerializeField]
    private Button button;
    [SerializeField]
    private TMP_Text text;
    [SerializeField]
    private string joinText = "Join";
    [SerializeField]
    private string leaveText = "Leave";
    
    public void Start()
    {
        leaderboardObject = (LeaderboardObject)Networking.FindComponentInPlayerObjects(Networking.LocalPlayer, leaderboardObject);
        text.text = leaderboardObject.hasJoined ? leaveText : joinText;
    }

    public void OnClick()
    {
        if(isLocked)
        {
            return;
        }
        
        if(leaderboardObject.hasJoined)
        {
            leaderboardObject._Leave();
            text.text = joinText;
        }else
        {
            leaderboardObject._Join();
            text.text = leaveText;
        }
        
        isLocked = true;
        button.interactable = false;
        SendCustomEventDelayedSeconds(nameof(_Unlock), 1);
    }
    
    public void _Unlock()
    {
        isLocked = false;
        button.interactable = true;
    }
}
