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
    //[SerializeField] public TMP_Text preference;
    [SerializeField] public TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Child mesh visible above this player's head only on the selected dancer's screen.")]
    [SerializeField] private GameObject selectionMesh;

    [Tooltip("Child checkmark mesh visible above audience member's head only on the dancing client who fulfilled the dance.")]
    [SerializeField] private GameObject danceFulfilledMesh;

    [Tooltip("Must match the exact name of the OverHeadDisplaysManager GameObject in your scene.")]
    public string managerObjectName = "OverHeadDisplaysManager";

    private OverHeadDisplaysManager manager;
    private VRCPlayerApi player;
    private int ownerPlayerId = -1;

    // Tracks which audience player IDs currently have THIS dancer selected.
    private int[] _activeSelectors;
    private int _activeSelectorCount = 0;

    // Tracks dancer toggle state so audience OHDs don't sweep meshes on every change.
    private bool _wasOwnerDancer = false;

    private bool _isEnabled = false;
    private bool IsMasterRestored = false;
    private bool IsLocalRestored = false;

    private Color red    = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private Color green  = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    private Color white  = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    // VRC Persistence Strings
    private const string KEY_OVERHEAD_DISPLAYS = "Codeyflex.DanceEventSuite.OverHeadDisplays";
    private const string KEY_OVERHEAD_DISPLAYS_COUNT = "Codeyflex.DanceEventSuite.OverHeadDisplaysCount";
    private const string KEY_OVERHEAD_DISPLAYS_START = "Codeyflex.DanceEventSuite.OverHeadDisplaysStartTime";
    private const string KEY_SELECTED_DANCER = "Codeyflex.DanceEventSuite.SelectedDancer";
    private const string KEY_NO_DANCES = "Codeyflex.DanceEventSuite.NoDances";
    private const string KEY_STAFF_MODE = "Codeyflex.DanceEventSuite.StaffMode";
    private const string KEY_DANCED_FOR = "Codeyflex.DanceEventSuite.DancedFor";
    private const string KEY_REQUEST_FULFILLED = "Codeyflex.DanceEventSuite.RequestFulfilled";
    private const string KEY_MEDIA_MODE = "Codeyflex.DanceEventSuite.MediaMode";

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
        danceFulfilledMesh.SetActive(false);
        UpdateEnabled();
        //SetPreference();

        int savedCount = PlayerData.GetInt(player, KEY_OVERHEAD_DISPLAYS_COUNT);
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
        if (danceFulfilledMesh.activeSelf) return;
        selectionMesh.SetActive(true);
        Debug.Log($"[OHD] ShowSelectionMesh called on {player.displayName} (id:{ownerPlayerId})");
    }

    public void HideSelectionMesh()
    {
        selectionMesh.SetActive(false);
        Debug.Log($"[OHD] HideSelectionMesh called on {player.displayName} (id:{ownerPlayerId})");
    }

    public void ShowDanceFulfilledMesh()
    {
        selectionMesh.SetActive(false);
        danceFulfilledMesh.SetActive(true);
    }

    public void HideDanceFulfilledMesh()
    {
        danceFulfilledMesh.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // VRChat callbacks
    // -----------------------------------------------------------------------

    private bool IsLocalDancer()
    {
        return player.isLocal && PlayerData.GetBool(player, KEY_OVERHEAD_DISPLAYS);
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player1, PlayerData.Info[] infos)
    {
        foreach (PlayerData.Info info in infos)
        {
            if (info.Key == KEY_OVERHEAD_DISPLAYS)
            {
                UpdateEnabled();
                OnDeserialization();
            }
            /*
            if (info.Key == "Talox.DancerGuidance.Preference")
                SetPreference();
            */
            if (info.Key == KEY_SELECTED_DANCER)
            {
                if (manager == null) continue;
                if (!IsLocalDancer()) continue;
                if (player1.playerId == ownerPlayerId) continue;

                int selectedDancerId = PlayerData.GetInt(player1, KEY_SELECTED_DANCER);
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

            if (info.Key == KEY_NO_DANCES)
            {
                if (player1.playerId == ownerPlayerId)
                    OnDeserialization();

                if (player1.isLocal && PlayerData.GetBool(player1, KEY_NO_DANCES))
                    PlayerData.SetInt(KEY_SELECTED_DANCER, 0);

                if (!IsLocalDancer()) continue;
                string dancedFor = PlayerData.GetString(player, KEY_DANCED_FOR);
                RefreshSingleDanceFulfilledMesh(dancedFor, player1);
            }

            if (info.Key == KEY_STAFF_MODE)
            {
                UpdateEnabled();
                OnDeserialization();
            }

            if (info.Key == KEY_MEDIA_MODE)
            {
                UpdateEnabled();
                OnDeserialization();
            }

            if (info.Key == KEY_DANCED_FOR)
            {
                if (!IsLocalDancer()) continue;
                string dancedFor = PlayerData.GetString(player, KEY_DANCED_FOR);
                RefreshDanceFulfilledMeshes(dancedFor);
            }

            if (info.Key == KEY_OVERHEAD_DISPLAYS_COUNT)
            {
                if (!IsLocalDancer()) continue;
                string dancedFor = PlayerData.GetString(player, KEY_DANCED_FOR);
                RefreshSingleDanceFulfilledMesh(dancedFor, player1);
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi restoredPlayer)
    {
        if (player.isLocal)
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
                    PlayerData.SetLong(KEY_OVERHEAD_DISPLAYS_START, DateTime.UtcNow.Ticks);
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
        if (manager != null && PlayerData.GetBool(player, KEY_OVERHEAD_DISPLAYS))
        {
            int selectedDancerId = PlayerData.GetInt(restoredPlayer, KEY_SELECTED_DANCER);
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

        if (restoredPlayer.isLocal && PlayerData.GetBool(restoredPlayer, KEY_OVERHEAD_DISPLAYS))
        {
            string dancedFor = PlayerData.GetString(restoredPlayer, KEY_DANCED_FOR);
            RefreshDanceFulfilledMeshes(dancedFor);
        }

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

    private void ClearSelectionMeshes()
    {
        int count = manager.GetCount();
        OverHeadDisplays[] all = manager.GetAll();
        for (int i = 0; i < count; i++)
            if (all[i] != null) all[i].HideSelectionMesh();
        _activeSelectorCount = 0;
    }

    private void RestoreSelectionMeshes()
    {
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        foreach (VRCPlayerApi p in players)
        {
            if (!Utilities.IsValid(p)) continue;
            if (p.playerId == ownerPlayerId) continue;
            if (PlayerData.GetInt(p, KEY_SELECTED_DANCER) == ownerPlayerId)
            {
                OverHeadDisplays selectorOHD = manager.GetForPlayer(p);
                if (selectorOHD != null)
                {
                    AddSelector(p.playerId);
                    selectorOHD.ShowSelectionMesh();
                }
            }
        }
    }

    private void CheckStartTime()
    {
        DateTime masterStartTime = new DateTime(PlayerData.GetLong(Networking.Master, KEY_OVERHEAD_DISPLAYS_START));
        DateTime localStartTime  = new DateTime(PlayerData.GetLong(player, KEY_OVERHEAD_DISPLAYS_START));

        if (player.isMaster) masterStartTime = DateTime.UtcNow;

        Debug.Log($"MasterStartTime: {masterStartTime} LocalStartTime: {localStartTime} " +
                  $"Diff: {masterStartTime - localStartTime} KeepAlive: {TimeSpan.FromHours(keepAlive)}");

        if (masterStartTime - (localStartTime + TimeSpan.FromHours(keepAlive)) > TimeSpan.Zero)
        {
            PlayerData.SetLong(KEY_OVERHEAD_DISPLAYS_START, masterStartTime.Ticks);
            number = 0;
            PlayerData.SetInt(KEY_OVERHEAD_DISPLAYS_COUNT, 0);
            PlayerData.SetBool(KEY_REQUEST_FULFILLED, false);
            PlayerData.SetBool(KEY_NO_DANCES, false);
            PlayerData.SetString(KEY_DANCED_FOR, "");
            if (ResetEnabledAfterEvent)
                PlayerData.SetBool(KEY_OVERHEAD_DISPLAYS, false);
            RequestSerialization();
        }
        else
        {
            number = PlayerData.GetInt(player, KEY_OVERHEAD_DISPLAYS_COUNT);
            RequestSerialization();
        }
    }

    public override void OnDeserialization()
    {
        if (PlayerData.GetBool(player, KEY_NO_DANCES))
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

        if (!_isEnabled) text.text = "";
    }

    public void OnClick()
    {
        if (!CanClick) return;

        if (PlayerData.GetBool(player, KEY_NO_DANCES)) return;

        if (!player.isLocal)
        {
            if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick) return;
            ShowDanceFulfilledMesh();
            AppendToDancedForList(ownerPlayerId);
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OnClick));
            CanClick = false;
            return;
        }

        CanClick = false;

        // If this audience member had a dancer selected, clear it and lock further
        // selections for the rest of this event. This runs on the owner's (Audience Members)
        // client — the only client that can write Audience Members own PlayerData.
        if (PlayerData.GetInt(Networking.LocalPlayer, KEY_SELECTED_DANCER) != 0)
        {
            PlayerData.SetInt(KEY_SELECTED_DANCER, 0);
            PlayerData.SetBool(KEY_REQUEST_FULFILLED, true);
        }

        int nextNumber = CalculateNextNumberState(number);
        text.text = GetDisplayTextForNumber(nextNumber);
        buttonImage.color = GetColorForNumber(nextNumber);
        number = nextNumber;
        PlayerData.SetInt(KEY_OVERHEAD_DISPLAYS_COUNT, number);
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
        _isEnabled = PlayerData.GetBool(Networking.LocalPlayer, KEY_OVERHEAD_DISPLAYS) ||
                     PlayerData.GetBool(Networking.LocalPlayer, KEY_STAFF_MODE);
        bool ownerEnabled = PlayerData.GetBool(player, KEY_OVERHEAD_DISPLAYS);
        bool ownerStaff = PlayerData.GetBool(player, KEY_STAFF_MODE);
        bool ownerMedia = PlayerData.GetBool(player, KEY_MEDIA_MODE);
        bool visible = !ownerEnabled & !ownerStaff & !ownerMedia & _isEnabled;
        canvasGroup.alpha = visible ? 1 : 0;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        BoxCollider col = (BoxCollider)GetComponent(typeof(BoxCollider));
        if (col != null) col.enabled = visible;

        if (_wasOwnerDancer && !ownerEnabled && manager != null)
            ClearSelectionMeshes();

        if (!_wasOwnerDancer && ownerEnabled && manager != null)
        {
            RestoreSelectionMeshes();
            string dancedFor = PlayerData.GetString(player, KEY_DANCED_FOR);
            RefreshDanceFulfilledMeshes(dancedFor);
        }

        _wasOwnerDancer = ownerEnabled;
    }
    /*
    private void SetPreference()
    {
        preference.text = PlayerData.GetString(player, "Talox.DancerGuidance.Preference");
    }
    */
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

    // -----------------------------------------------------------------------
    // DanceFulfilledMesh helpers (local client only)
    // -----------------------------------------------------------------------

    private bool ShouldShowDanceFulfilledMesh(string dancedFor, OverHeadDisplays ohd)
    {
        return IsIdInList(dancedFor, ohd.GetOwnerPlayerId())
            && ohd.number < maxDances
            && !PlayerData.GetBool(ohd.GetOwnerPlayer(), KEY_NO_DANCES);
    }

    private void RefreshDanceFulfilledMeshes(string dancedFor)
    {
        if (manager == null) return;
        int count = manager.GetCount();
        OverHeadDisplays[] all = manager.GetAll();
        for (int i = 0; i < count; i++)
        {
            OverHeadDisplays ohd = all[i];
            if (ohd == null) continue;
            if (ShouldShowDanceFulfilledMesh(dancedFor, ohd))
                ohd.ShowDanceFulfilledMesh();
            else
                ohd.HideDanceFulfilledMesh();
        }
    }

    private void RefreshSingleDanceFulfilledMesh(string dancedFor, VRCPlayerApi audiencePlayer)
    {
        if (manager == null) return;
        OverHeadDisplays ohd = manager.GetForPlayer(audiencePlayer);
        if (ohd == null) return;
        if (ShouldShowDanceFulfilledMesh(dancedFor, ohd))
            ohd.ShowDanceFulfilledMesh();
        else
            ohd.HideDanceFulfilledMesh();
    }

    private void AppendToDancedForList(int playerId)
    {
        string existing = PlayerData.GetString(Networking.LocalPlayer, KEY_DANCED_FOR);
        if (existing == null) existing = "";
        if (IsIdInList(existing, playerId)) return;

        string idStr = playerId.ToString();
        string updated = existing.Length > 0 ? existing + " " + idStr : idStr;
        PlayerData.SetString(KEY_DANCED_FOR, updated);
    }

    private bool IsIdInList(string list, int playerId)
    {
        if (list == null || list.Length == 0) return false;

        string idStr = playerId.ToString();
        int listLen = list.Length;
        int idLen = idStr.Length;

        for (int i = 0; i <= listLen - idLen; i++)
        {
            bool match = true;
            for (int j = 0; j < idLen; j++)
            {
                if (list[i + j] != idStr[j]) { match = false; break; }
            }
            if (!match) continue;

            bool startBoundary = i == 0 || list[i - 1] == ' ';
            bool endBoundary = i + idLen == listLen || list[i + idLen] == ' ';
            if (startBoundary && endBoundary) return true;
        }
        return false;
    }
}
