
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class RankDisplay : UdonSharpBehaviour
{
    [UdonSynced]
    public int currentRankIndex = -1;
    
    [SerializeField]
    public DancerRank dancerRank;
    
    private GameObject currentRankDisplay;

    public override void OnDeserialization()
    {
        UpdateRankDisplay();
    }

    public void SetRank(int rankIndex)
    {
        currentRankIndex = rankIndex;
        Debug.Log("SetRank: " + currentRankIndex);
        RequestSerialization();
        UpdateRankDisplay();
    }

    public void UpdateRankDisplay()
    {
        if (currentRankDisplay != null)
        {
            Destroy(currentRankDisplay);
        }
        
        GameObject rankDisplayPrefab = dancerRank.GetRankDisplay(currentRankIndex);
        if(rankDisplayPrefab == null)
        {
            return;
        }
        
        currentRankDisplay = Instantiate(rankDisplayPrefab, transform);
        currentRankDisplay.transform.localPosition = Vector3.zero;
        currentRankDisplay.transform.localRotation = Quaternion.identity;
        currentRankDisplay.transform.localScale = Vector3.one*500;
        
        currentRankDisplay.SetActive(true);
    }
}
