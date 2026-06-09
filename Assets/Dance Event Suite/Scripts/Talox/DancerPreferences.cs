
using System;
using System.Text;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

public class DancerPreferences : UdonSharpBehaviour
{
    [SerializeField]
    private string[] preferenceNames;
    
    [SerializeField]
    private DancerPreferencesButton preferenceButtonPrefab;
    [SerializeField]
    private Transform preferenceButtonParent;
    
    private DancerPreferencesButton[] preferenceButtons;


    private DataList localPreferences = new DataList();
    
    public void Start()
    {
        GenerateRankButtons();
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if(player.playerId != Networking.LocalPlayer.playerId) return;
        
        foreach (PlayerData.Info info in infos)
        {
            if (info.Key == "Talox.DancerGuidance.Preference")
            {
                localPreferences = new DataList();
                string[] preferences = PlayerData.GetString(Networking.LocalPlayer, "Talox.DancerGuidance.Preference")
                    .Split('\n');
                
                for (int i = 0; i < preferences.Length; i++)
                { 
                    int index = Array.IndexOf(preferenceNames, preferences[i]);
                    if (index >= 0)
                    {
                        localPreferences.Add(index);
                    }
                }

                for (int i = 0; i < localPreferences.Count; i++)
                {
                    preferenceButtons[localPreferences[i].Int].SetValue(true);
                }
            }
        }
    }

    public void GenerateRankButtons()
    {
        preferenceButtons = new DancerPreferencesButton[preferenceNames.Length];
        for (int i = 0; i < preferenceNames.Length; i++)
        {
            DancerPreferencesButton rankButton = Instantiate(preferenceButtonPrefab.gameObject,preferenceButtonParent).GetComponent<DancerPreferencesButton>();
            rankButton.Initialize(i, preferenceNames[i]);
            rankButton.gameObject.SetActive(true);
            preferenceButtons[i] = rankButton;
        }
    }
    
    public void SetRank(int rankIndex)
    {
        if (localPreferences.Contains(rankIndex))
        {
            preferenceButtons[rankIndex].SetValue(false);
            localPreferences.RemoveAt(localPreferences.IndexOf(rankIndex));
        }
        else
        {
            preferenceButtons[rankIndex].SetValue(true);
            localPreferences.Add(rankIndex);
        }
        
        StringBuilder stringBuilder = new StringBuilder();

        for (int i = 0; i < localPreferences.Count; i++)
        {
            stringBuilder.Append(preferenceNames[localPreferences[i].Int]);
            if(i < localPreferences.Count - 1)
                stringBuilder.Append("\n");
        }

        PlayerData.SetString("Talox.DancerGuidance.Preference", stringBuilder.ToString());
    }
}
