using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// -----------------------------------------------------------------------
// OHD Registry — lookup all player OHD instances
// -----------------------------------------------------------------------
// Fixed-size array (U# can't resize). GetForPlayer uses stored ownerPlayerId
// (int) rather than VRCPlayerApi.playerId because the stored reference can
// return wrong values for remote players at runtime.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OverHeadDisplaysManager : UdonSharpBehaviour
{
    private OverHeadDisplays[] _registered = new OverHeadDisplays[100];
    private int _count = 0;

    public void Register(OverHeadDisplays ohd)
    {
        if (_count >= _registered.Length)
        {
            Debug.LogWarning("[OverHeadDisplaysManager] Registration array full.");
            return;
        }
        _registered[_count] = ohd;
        _count++;
        Debug.Log($"[OverHeadDisplaysManager] Registered OHD #{_count} owner: {ohd.GetOwnerPlayer().displayName} (id: {ohd.GetOwnerPlayerId()})");
    }

    // Uses GetOwnerPlayerId() (a stored int) rather than GetOwnerPlayer().playerId,
    // because the stored VRCPlayerApi reference can return wrong playerId values
    // for remote player objects at runtime.
    public OverHeadDisplays GetForPlayer(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return null;

        int targetId = player.playerId;
        for (int i = 0; i < _count; i++)
        {
            if (_registered[i] == null) continue;
            if (_registered[i].GetOwnerPlayerId() == targetId)
                return _registered[i];
        }

        Debug.LogWarning($"[OverHeadDisplaysManager] No OHD found for player: {player.displayName} (id: {player.playerId})");
        return null;
    }

    public void Unregister(OverHeadDisplays ohd)
    {
        for (int i = 0; i < _registered.Length; i++)
        {
            if (_registered[i] == ohd)
            {
                _registered[i] = null;
                return;
            }
        }
    }

    public OverHeadDisplays[] GetAll() => _registered;
    public int GetCount() => _count;
}
