using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class OverHeadDisplays : UdonSharpBehaviour
{
    [UdonSynced] public int number;
    [UdonSynced] private bool CanClick = true;

    public Vector3 offset;
    public int dancesNeeded = 2;
    public int dancesNoLongerNeeded = 5;
    public int maxDances = 10;
    public string noDancesText = "ND";
    public float MaxDistanceForClick = 5.0f;
    public float ClickDelay = 0.1f;
    public float keepAlive = 8f;
    public bool ResetEnabledAfterEvent = false;
    public bool LookAtPlayers = false;

    [SerializeField] private UnityEngine.UI.Image buttonImage;
    [SerializeField] public TMP_Text preference;
    [SerializeField] public TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Child mesh visible above this player's head only on the selected dancer's screen.")]
    [SerializeField] private GameObject selectionMesh;

    [Tooltip("Must match the exact name of the OverHeadDisplaysManager GameObject in your scene.")]
    public string managerObjectName = "OverHeadDisplaysManager";

    private OverHeadDisplaysManager manager;
    private VRCPlayerApi player;
    private int ownerPlayerId = -1;

    // Tracks which audience player IDs currently have THIS dancer selected.
    private int[] _activeSelectors;
    private int _activeSelectorCount = 0;

    // Tracks the previous dancer-enabled state so UpdateEnabled only sweeps
    // and clears selection meshes when a dancer specifically turns their toggle OFF.
    // Without this, audience OHDs (always ownerEnabled=false) would sweep and
    // hide every selection mesh every time any player's OverHeadDisplays key changes.
    private bool _wasOwnerDancer = false;

    private bool IsEnabled = false;
    private bool IsMasterRestored = false;
    private bool IsLocalRestored = false;

    private Color red    = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private Color green  = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    private Color white  = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Start()
    {
        player = Networking.GetOwner(gameObject);
        ownerPlayerId = player.playerId;
        _activeSelectors = new int[82];

        GameObject managerObj = GameObject.Find(managerObjectName);
        if (managerObj != null)
            manager = managerObj.GetComponent<OverHeadDisplaysManager>();

        if (manager == null)
            Debug.LogError($"[OverHeadDisplays] Could not find OverHeadDisplaysManager named '{managerObjectName}'.");
        else
        {
            manager.Register(this);
            Debug.Log($"[OverHeadDisplays] Registered for player: {player.displayName} (id: {ownerPlayerId})");
        }

        selectionMesh.SetActive(false);
        UpdateEnabled();
        SetPreference();

        int savedCount = PlayerData.GetInt(player, "Codeyflex.DanceEventSuite.OverHeadDisplaysCount");
        number = savedCount > 0 ? savedCount : 0;
        RequestSerialization();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        player = newOwner;
        ownerPlayerId = newOwner.playerId;
    }

    public override void OnPlayerLeft(VRCPlayerApi leftPlayer)
    {
        RemoveSelector(leftPlayer.playerId);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.Unregister(this);
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    public int GetOwnerPlayerId() => ownerPlayerId;
    public VRCPlayerApi GetOwnerPlayer() => player;

    public void ShowSelectionMesh()
    {
        selectionMesh.SetActive(true);
        Debug.Log($"[OHD] ShowSelectionMesh called on {player.displayName} (id:{ownerPlayerId})");
    }

    public void HideSelectionMesh()
    {
        selectionMesh.SetActive(false);
        Debug.Log($"[OHD] HideSelectionMesh called on {player.displayName} (id:{ownerPlayerId})");
    }

    // -----------------------------------------------------------------------
    // VRChat callbacks
    // -----------------------------------------------------------------------

    public override void OnPlayerDataUpdated(VRCPlayerApi player1, PlayerData.Info[] infos)
    {
        foreach (PlayerData.Info info in infos)
        {
            if (info.Key == "Codeyflex.DanceEventSuite.OverHeadDisplays")
            {
                UpdateEnabled();
                OnDeserialization();
            }

            if (info.Key == "Talox.DancerGuidance.Preference")
                SetPreference();

            if (info.Key == "Codeyflex.DanceEventSuite.SelectedDancer")
            {
                if (manager == null) continue;

                // OnPlayerDataUpdated fires on every client for every OHD instance.
                // Without this guard, Dancer's OHD would run on Audience members client too,
                // see selectedDancerId == Dancer's ownerPlayerId, and show audience members mesh
                // for Audience members — making the mesh visible to all dancers, not just Dancers.
                // Each client should only process selection events through its own
                // local player's OHD instance.
                if (!player.isLocal) continue;

                bool ownerIsDancer = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.OverHeadDisplays");
                if (!ownerIsDancer) continue;

                if (player1.playerId == ownerPlayerId) continue;

                int selectedDancerId = PlayerData.GetInt(player1, "Codeyflex.DanceEventSuite.SelectedDancer");
                bool player1HadMeSelected = ContainsSelector(player1.playerId);

                if (selectedDancerId == ownerPlayerId)
                {
                    OverHeadDisplays selectorOHD = manager.GetForPlayer(player1);
                    if (selectorOHD == null)
                    {
                        Debug.LogWarning($"[OHD:{player.displayName} id:{ownerPlayerId}] No OHD found for selector {player1.displayName}.");
                        continue;
                    }
                    AddSelector(player1.playerId);
                    selectorOHD.ShowSelectionMesh();
                    Debug.Log($"[OHD:{player.displayName} id:{ownerPlayerId}] {player1.displayName} selected us. Active selectors: {_activeSelectorCount}");
                }
                else if (player1HadMeSelected)
                {
                    OverHeadDisplays selectorOHD = manager.GetForPlayer(player1);
                    if (selectorOHD == null) continue;
                    RemoveSelector(player1.playerId);
                    selectorOHD.HideSelectionMesh();
                    Debug.Log($"[OHD:{player.displayName} id:{ownerPlayerId}] {player1.displayName} deselected us. Active selectors: {_activeSelectorCount}");
                }
            }

            if (info.Key == "Codeyflex.DanceEventSuite.NoDances")
            {
                if (player1.playerId == ownerPlayerId)
                    OnDeserialization();

                if (player1.isLocal && PlayerData.GetBool(player1, "Codeyflex.DanceEventSuite.NoDances"))
                    PlayerData.SetInt("Codeyflex.DanceEventSuite.SelectedDancer", 0);
            }

            if (info.Key == "Codeyflex.DanceEventSuite.StaffMode")
            {
                UpdateEnabled();
                OnDeserialization();
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi restoredPlayer)
    {
        if (this.player.isLocal)
        {
            if (restoredPlayer.isLocal)
            {
                IsLocalRestored = true;
                if (IsMasterRestored) CheckStartTime();
            }

            if (restoredPlayer.isMaster)
            {
                if (restoredPlayer.isLocal)
                {
                    CheckStartTime();
                    PlayerData.SetLong("Codeyflex.DanceEventSuite.OverHeadDisplaysStartTime", DateTime.Now.Ticks);
                }
                else
                {
                    IsMasterRestored = true;
                    if (IsLocalRestored) CheckStartTime();
                }
            }
        }

        // Only restore selection mesh state for players who had this dancer selected.
        // Skipping the else-hide: meshes start hidden in Start(), so no need to
        // explicitly hide for players who didn't select this dancer, and doing so
        // risks incorrectly hiding another audience member's mesh if the manager
        // lookup returns an unexpected result.
        if (manager != null && PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.OverHeadDisplays"))
        {
            int selectedDancerId = PlayerData.GetInt(restoredPlayer, "Codeyflex.DanceEventSuite.SelectedDancer");
            if (selectedDancerId == ownerPlayerId)
            {
                OverHeadDisplays restoredOHD = manager.GetForPlayer(restoredPlayer);
                if (restoredOHD != null)
                {
                    AddSelector(restoredPlayer.playerId);
                    restoredOHD.ShowSelectionMesh();
                }
            }
        }

        if (restoredPlayer.playerId == ownerPlayerId)
            OnDeserialization();

        UpdateEnabled();
    }

    // -----------------------------------------------------------------------
    // Selector tracking helpers
    // -----------------------------------------------------------------------

    private bool ContainsSelector(int playerId)
    {
        for (int i = 0; i < _activeSelectorCount; i++)
            if (_activeSelectors[i] == playerId) return true;
        return false;
    }

    private void AddSelector(int playerId)
    {
        if (ContainsSelector(playerId)) return;
        if (_activeSelectorCount >= _activeSelectors.Length) return;
        _activeSelectors[_activeSelectorCount] = playerId;
        _activeSelectorCount++;
    }

    private void RemoveSelector(int playerId)
    {
        for (int i = 0; i < _activeSelectorCount; i++)
        {
            if (_activeSelectors[i] == playerId)
            {
                _activeSelectors[i] = _activeSelectors[_activeSelectorCount - 1];
                _activeSelectorCount--;
                return;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Existing logic (unchanged)
    // -----------------------------------------------------------------------

    private void CheckStartTime()
    {
        DateTime masterStartTime = new DateTime(PlayerData.GetLong(Networking.Master, "Codeyflex.DanceEventSuite.OverHeadDisplaysStartTime"));
        DateTime localStartTime  = new DateTime(PlayerData.GetLong(player, "Codeyflex.DanceEventSuite.OverHeadDisplaysStartTime"));

        if (player.isMaster) masterStartTime = DateTime.Now;

        Debug.Log($"MasterStartTime: {masterStartTime} LocalStartTime: {localStartTime} " +
                  $"Diff: {masterStartTime - localStartTime} KeepAlive: {TimeSpan.FromHours(keepAlive)}");

        if (masterStartTime - (localStartTime + TimeSpan.FromHours(keepAlive)) > TimeSpan.Zero)
        {
            PlayerData.SetLong("Codeyflex.DanceEventSuite.OverHeadDisplaysStartTime", masterStartTime.Ticks);
            number = 0;
            PlayerData.SetInt("Codeyflex.DanceEventSuite.OverHeadDisplaysCount", 0);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.RequestFulfilled", false);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.NoDances", false);
            if (ResetEnabledAfterEvent)
                PlayerData.SetBool("Codeyflex.DanceEventSuite.OverHeadDisplays", false);
            RequestSerialization();
        }
        else
        {
            number = PlayerData.GetInt(player, "Codeyflex.DanceEventSuite.OverHeadDisplaysCount");
            RequestSerialization();
        }
    }

    public override void OnDeserialization()
    {
        if (PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.NoDances"))
        {
            text.text = noDancesText;
            buttonImage.color = GetColorForNumber(maxDances);
        }
        else
        {
            text.text = GetDisplayTextForNumber(number);
            buttonImage.color = GetColorForNumber(number);
        }
    }

    public void Update()
    {
        VRCPlayerApi.TrackingData headOwner       = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        VRCPlayerApi.TrackingData headLocalPlayer = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        transform.position = headOwner.position + offset;

        Vector3 lookDirection = -headLocalPlayer.position + transform.position;
        if (LookAtPlayers)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
        else
        {
            lookDirection.y = 0;
            lookDirection.Normalize();
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        if (!IsEnabled) text.text = "";
    }

    public void OnClick()
    {
        if (!CanClick) return;

        if (PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.NoDances")) return;

        if (!player.isLocal)
        {
            if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OnClick));
            CanClick = false;
            return;
        }

        CanClick = false;

        // If this audience member had a dancer selected, clear it and lock further
        // selections for the rest of this event. This runs on the owner's (Audience Members)
        // client — the only client that can write Audience Members own PlayerData.
        if (PlayerData.GetInt(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.SelectedDancer") != 0)
        {
            PlayerData.SetInt("Codeyflex.DanceEventSuite.SelectedDancer", 0);
            PlayerData.SetBool("Codeyflex.DanceEventSuite.RequestFulfilled", true);
        }

        int nextNumber = CalculateNextNumberState(number);
        text.text = GetDisplayTextForNumber(nextNumber);
        buttonImage.color = GetColorForNumber(nextNumber);
        number = nextNumber;
        PlayerData.SetInt("Codeyflex.DanceEventSuite.OverHeadDisplaysCount", number);
        RequestSerialization();
        SendCustomEventDelayedSeconds(nameof(OnClickEnd), ClickDelay);
    }

    public void OnClickEnd()
    {
        CanClick = true;
        RequestSerialization();
    }

    private void UpdateEnabled()
    {
        IsEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.OverHeadDisplays") ||
                    PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.StaffMode");
        bool ownerEnabled = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.OverHeadDisplays");
        bool ownerStaff = PlayerData.GetBool(player, "Codeyflex.DanceEventSuite.StaffMode");
        canvasGroup.alpha = !ownerEnabled & !ownerStaff & IsEnabled ? 1 : 0;

        // Only sweep and clear selection meshes if THIS dancer specifically just
        // turned their toggle OFF. Without _wasOwnerDancer, audience OHDs
        // (always ownerEnabled=false) would sweep all meshes on every update.
        if (_wasOwnerDancer && !ownerEnabled && manager != null)
        {
            int count = manager.GetCount();
            OverHeadDisplays[] all = manager.GetAll();
            for (int i = 0; i < count; i++)
                if (all[i] != null) all[i].HideSelectionMesh();
            _activeSelectorCount = 0;
            Debug.Log($"[OHD:{player.displayName} id:{ownerPlayerId}] Dancer deactivated, cleared all selection meshes.");
        }

        _wasOwnerDancer = ownerEnabled;
    }

    private void SetPreference()
    {
        preference.text = PlayerData.GetString(player, "Talox.DancerGuidance.Preference");
    }

    private int CalculateNextNumberState(int currentNumber)
    {
        if (currentNumber < maxDances) return currentNumber + 1;
        else if (currentNumber == maxDances + 1) return maxDances + 1;
        else return 0;
    }

    private string GetDisplayTextForNumber(int num)
    {
        return num >= maxDances ? noDancesText : num.ToString();
    }

    private Color GetColorForNumber(int num)
    {
        if (num == 0) return white;
        else if (num > dancesNoLongerNeeded) return red;
        else if (num > dancesNeeded && num <= dancesNoLongerNeeded) return orange;
        else return green;
    }
}
