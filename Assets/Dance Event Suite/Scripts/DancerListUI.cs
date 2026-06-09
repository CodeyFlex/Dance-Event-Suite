using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DancerListUI : UdonSharpBehaviour
{
    [SerializeField] private OverHeadDisplaysManager manager;

    [Tooltip("DancerListButton on each pre-allocated slot. Same order as buttonImages and buttonLabels.")]
    private DancerListButton[] buttons;

    [Tooltip("Image (background) on each slot. Same order as buttons and buttonLabels.")]
    [SerializeField] private Image[] buttonImages;

    [Tooltip("TMP_Text on each slot. Same order as buttons and buttonImages.")]
    [SerializeField] private TMP_Text[] buttonLabels;

    private Color _selectedColour   = new Color(0.0f, 0.8f, 0.2f, 1.0f);
    private Color _unselectedColour = new Color(0.8f, 0.1f, 0.1f, 1.0f);
    private Color _lockedColour     = new Color(0.35f, 0.35f, 0.35f, 1.0f);

    // Tracks which dancer ID each visible slot currently represents.
    // Used for button colour logic. Populated in RefreshList alongside SetDancerId.
    private int[] _slotDancerIds;

    private void Start()
    {
        buttons = new DancerListButton[buttonImages.Length];
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] != null)
                buttons[i] = buttonImages[i].GetComponent<DancerListButton>();
        }

        _slotDancerIds = new int[buttons.Length];
        HideAllSlots();
        RefreshList();
        SendCustomEventDelayedSeconds(nameof(DelayedRefresh), 3.0f);
    }

    public void DelayedRefresh()
    {
        RefreshList();
    }

    // -----------------------------------------------------------------------
    // VRChat callbacks
    // -----------------------------------------------------------------------

    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        foreach (PlayerData.Info info in infos)
        {
            if (info.Key == "Codeyflex.DanceEventSuite.OverHeadDisplays")
            {
                RefreshList();
                continue;
            }

            if (info.Key == "Codeyflex.DanceEventSuite.SelectedDancer" && player.isLocal)
            {
                RefreshButtonColours();
                continue;
            }

            if (info.Key == "Codeyflex.DanceEventSuite.RequestFulfilled" && player.isLocal)
            {
                RefreshButtonColours();
                continue;
            }
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        RefreshList();
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        RefreshList();
    }

    // -----------------------------------------------------------------------
    // Called by DancerListButton — receives dancer ID directly, no index needed
    // -----------------------------------------------------------------------

    public void OnDancerSelected(int dancerId)
    {
        if (dancerId == 0) return; // Slot not currently populated.

        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.OverHeadDisplays"))
            return; // Dancers cannot make selections.

        if (PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.RequestFulfilled"))
            return; // Request already fulfilled this event.

        int currentSelectionId = PlayerData.GetInt(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.SelectedDancer");

        if (currentSelectionId == dancerId)
        {
            Debug.Log($"[DancerListUI] Deselecting dancer id {dancerId}");
            PlayerData.SetInt("Codeyflex.DanceEventSuite.SelectedDancer", 0);
        }
        else
        {
            Debug.Log($"[DancerListUI] Selecting dancer id {dancerId}");
            PlayerData.SetInt("Codeyflex.DanceEventSuite.SelectedDancer", dancerId);
        }
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private void RefreshList()
    {
        if (_slotDancerIds == null)
            _slotDancerIds = new int[buttons.Length];

        HideAllSlots();

        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        int slotIndex = 0;
        foreach (VRCPlayerApi p in players)
        {
            if (!Utilities.IsValid(p)) continue;
            if (!PlayerData.GetBool(p, "Codeyflex.DanceEventSuite.OverHeadDisplays")) continue;
            if (slotIndex >= buttons.Length) break;

            int dancerId = p.playerId;
            buttonImages[slotIndex].gameObject.SetActive(true);
            buttonLabels[slotIndex].text = p.displayName;
            buttons[slotIndex].SetDancerId(dancerId);
            _slotDancerIds[slotIndex] = dancerId;

            Debug.Log($"[DancerListUI] RefreshList: slot {slotIndex} = {p.displayName} (id:{dancerId})");
            slotIndex++;
        }

        RefreshButtonColours();
    }

    private void RefreshButtonColours()
    {
        if (_slotDancerIds == null) return;

        bool isLocked = PlayerData.GetBool(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.RequestFulfilled");
        int currentSelectionId = PlayerData.GetInt(Networking.LocalPlayer, "Codeyflex.DanceEventSuite.SelectedDancer");

        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (!buttonImages[i].gameObject.activeSelf) continue;

            if (isLocked)
            {
                buttonImages[i].color = _lockedColour;
                continue;
            }

            bool isSelected = _slotDancerIds[i] != 0 && _slotDancerIds[i] == currentSelectionId;
            buttonImages[i].color = isSelected ? _selectedColour : _unselectedColour;
        }
    }

    private void HideAllSlots()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttonImages[i].gameObject.SetActive(false);
            if (buttons   != null) buttons[i].SetDancerId(0);
            if (_slotDancerIds != null) _slotDancerIds[i] = 0;
        }
    }
}
