using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// Attach to each pre-allocated dancer list button slot.
// Wire the 'listUI' reference in the Inspector.
// The Unity Button's OnClick event should call SendCustomEvent "OnClick"
// Do NOT set anything else manually; DancerListUI.RefreshList() assigns the
// correct dancer ID at runtime via SetDancerId().
// -----------------------------------------------------------------------
// Individual dancer slot on the DancerListUI board
// -----------------------------------------------------------------------
// DancerListUI.RefreshList() assigns dancer IDs via SetDancerId() at
// runtime. OnClick forwards to DancerListUI.OnDancerSelected with the
// stored ID. 0 means unpopulated slot (hidden).
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DancerListButton : UdonSharpBehaviour
{
    [SerializeField] private DancerListUI listUI;
    public float MaxDistanceForClick = 5.0f;

    // Set by DancerListUI.RefreshList() each time the dancer list changes.
    // 0 means this slot is currently empty.
    private int _dancerId;

    public void SetDancerId(int id)
    {
        _dancerId = id;
    }

    public void OnClick()
    {
        if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;
        if (listUI == null) return;
        listUI.OnDancerSelected(_dancerId);
    }
}
