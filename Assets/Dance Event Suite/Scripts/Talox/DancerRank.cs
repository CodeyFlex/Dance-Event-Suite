
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DancerRank : UdonSharpBehaviour
{
    [SerializeField]
    private string[] rankNames;
    [SerializeField]
    private GameObject[] rankDisplay;
    [SerializeField] 
    private int[] rankOverrides;
    
    [SerializeField]
    private RankButton rankButtonPrefab;
    [SerializeField]
    private Transform rankButtonParent;
    [SerializeField]
    private RankDisplay RankDisplay;
    
    private RankDisplay localRankDisplay;
    
    private RankButton[] rankButtons;
    
    
    public void Start()
    {
        GenerateRankButtons();
        
        localRankDisplay = (RankDisplay)Networking.FindComponentInPlayerObjects(Networking.LocalPlayer, RankDisplay);
    }
    
    public void GenerateRankButtons()
    {
        rankButtons = new RankButton[rankNames.Length];
        for (int i = 0; i < rankNames.Length; i++)
        {
            RankButton rankButton = Instantiate(rankButtonPrefab.gameObject,rankButtonParent).GetComponent<RankButton>();
            rankButton.Initialize(i, rankNames[i]);
            rankButton.gameObject.SetActive(true);
            rankButtons[i] = rankButton;
        }
    }
    
    public void SetRank(int rankIndex)
    {
        for (int i = 0; i < rankButtons.Length; i++)
        {
            rankButtons[i].SetSelected(i == rankIndex);
        }
        
        localRankDisplay.SetRank(rankIndex);
        
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        foreach (VRCPlayerApi player in players)
        {
            if (player.isLocal)
            {
                continue;
            }
            
            RankDisplay rankDisplay = (RankDisplay)Networking.FindComponentInPlayerObjects(player, RankDisplay);
            if(rankDisplay != null)
            {
                rankDisplay.UpdateRankDisplay();
            }
        }
    }

    public GameObject GetRankDisplay(int currentRankIndex)
    {
        if (currentRankIndex < 0 || currentRankIndex >= rankDisplay.Length)
        {
            return null;
        }

        return rankDisplay[localRankDisplay.currentRankIndex == -1 ? rankOverrides[currentRankIndex] : currentRankIndex];
    }
}
