
using System;
using System.Management.Instrumentation;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class OverHeadNumber : UdonSharpBehaviour
{
    [UdonSynced]
    public int number;
    [UdonSynced]
    private bool CanClick = true;
    [UdonSynced]
    private int uniqueId;
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
    [SerializeField]
    private UnityEngine.UI.Image buttonImage;
    [SerializeField]
    public TMP_Text preference;
    [SerializeField]
    public TMP_Text text;
    [SerializeField]
    private CanvasGroup canvasGroup;

    private VRCPlayerApi player;
    private bool IsEnabled = false;

    private bool IsMasterRestored = false;
    private bool IsLocalRestored = false;

    private Color red = new Color(1.0f, 0.0f, 0.0f, 1.0f);
    private Color green = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    private Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    private Color white = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    private void Start()
    {
        player = Networking.GetOwner(gameObject);
        UpdateEnabled();
        SetPreference();

        // Number would disppear if display was toggled off and on, so to fix this:
        // Check if a saved count exists in Player Data for this user.
        int savedCount = PlayerData.GetInt(player, "Talox.DancerGuidance.OverHeadNumberCount");

        if (savedCount > 0)
        {
            // If we find a saved count, set the synced variable to it and request serialization
            // so all clients update immediately upon joining.
            number = savedCount;
            RequestSerialization();
        }
        else
        {
            // If no saved count, ensure it starts at the default value
            number = 0;
            RequestSerialization();
        }
    }

    public override void OnPlayerDataUpdated(VRCPlayerApi player1, PlayerData.Info[] infos)
    {
        foreach (PlayerData.Info info in infos)
        {
            if (info.Key == "Talox.DancerGuidance.OverHeadNumber")
            {
                UpdateEnabled();
                // When the toggle state changes, force a deserialization/update to refresh text color and content if needed.
                OnDeserialization();
            }

            if (info.Key == "Talox.DancerGuidance.Preference")
            {
                SetPreference();
            }
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (this.player.isLocal)
        {
            if (player.isLocal)
            {
                IsLocalRestored = true;

                if (IsMasterRestored)
                {
                    CheckStartTime();
                }
            }

            if (player.isMaster)
            {
                if (player.isLocal)
                {
                    CheckStartTime();
                    PlayerData.SetLong("Talox.DancerGuidance.OverHeadNumberStartTime", DateTime.UtcNow.Ticks);
                }
                else
                {
                    IsMasterRestored = true;
                    if (IsLocalRestored)
                        CheckStartTime();
                }
            }
        }

        UpdateEnabled();
    }

    private void CheckStartTime()
    {
        DateTime masterStartTime = new DateTime(PlayerData.GetLong(Networking.Master, "Talox.DancerGuidance.OverHeadNumberStartTime"));
        DateTime localStartTime = new DateTime(PlayerData.GetLong(player, "Talox.DancerGuidance.OverHeadNumberStartTime"));

        if (player.isMaster)
        {
            masterStartTime = DateTime.UtcNow;
        }

        Debug.Log($"MasterStartTime: {masterStartTime} LocalStartTime: {localStartTime} Diff: {masterStartTime - localStartTime} 8 Hours: {TimeSpan.FromHours(keepAlive)}");

        if (masterStartTime - (localStartTime + TimeSpan.FromHours(keepAlive)) > TimeSpan.Zero)
        {
            PlayerData.SetLong("Talox.DancerGuidance.OverHeadNumberStartTime", masterStartTime.Ticks);
            number = 0;
            PlayerData.SetInt("Talox.DancerGuidance.OverHeadNumberCount", 0);
            if (ResetEnabledAfterEvent)
            {
                PlayerData.SetBool("Talox.DancerGuidance.OverHeadNumber", false);
            }
            RequestSerialization();
            Debug.Log($"Number: {number} set to 0");
        }
        else
        {
            number = PlayerData.GetInt(player, "Talox.DancerGuidance.OverHeadNumberCount");
            RequestSerialization();
            Debug.Log($"Number: {number}");
        }
    }

    public override void OnDeserialization() // Triggers when data changes
    {
        text.text = GetDisplayTextForNumber(number);
        buttonImage.color = GetColorForNumber(number);
    }

    public void Update()
    {
        VRCPlayerApi.TrackingData HeadOwner = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        VRCPlayerApi.TrackingData HeadLocalPlayer = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        transform.position = HeadOwner.position + offset;
        Vector3 lookDirection = -HeadLocalPlayer.position + transform.position;
        if (LookAtPlayers)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
        else
        {
            lookDirection.y = 0; // Keep the text upright by ignoring vertical rotation
            lookDirection.Normalize();
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        if (!IsEnabled)
        {
            text.text = "";
        }
    }

    public void OnClick()
    {
        if (!CanClick)
            return;

        if (!player.isLocal)
        {
            if ((transform.position - Networking.LocalPlayer.GetPosition()).magnitude > MaxDistanceForClick)
                return;

            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(OnClick));
            CanClick = false;
            return;
        }

        CanClick = false;

        // 1. Calculate the next state based on current 'number'
        int nextNumber = CalculateNextNumberState(number);

        // 2. Update local UI immediately for responsiveness
        text.text = GetDisplayTextForNumber(nextNumber);
        buttonImage.color = GetColorForNumber(nextNumber);

        // 3. Update the synced variable and save it
        number = nextNumber;

        PlayerData.SetInt("Talox.DancerGuidance.OverHeadNumberCount", number);
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
        IsEnabled = PlayerData.GetBool(Networking.LocalPlayer, "Talox.DancerGuidance.OverHeadNumber");
        bool OwnerEnabled = PlayerData.GetBool(player, "Talox.DancerGuidance.OverHeadNumber");
        canvasGroup.alpha = !OwnerEnabled & IsEnabled ? 1 : 0;
    }

    private void SetPreference()
    {
        preference.text = PlayerData.GetString(player, "Talox.DancerGuidance.Preference");
    }

    // Calculates the next number based on current state
    private int CalculateNextNumberState(int currentNumber)
    {
        if (currentNumber < maxDances)
        {
            return currentNumber + 1;
        }
        else if (currentNumber == maxDances + 1)
        {
            // Jump directly to the state that represents "No dances"
            return maxDances + 1;
        }
        else // currentNumber >= maxDances (Reset path)
        {
            return 0; // Reset
        }
    }

    // Determines what text to show based on the number
    private string GetDisplayTextForNumber(int num)
    {
        if (num >= maxDances)
        {
            return noDancesText;
        }
        else
        {
            return num.ToString();
        }
    }

    // Determines the color based on the number ---
    private Color GetColorForNumber(int num)
    {
        if (num == 0) return white;
        else if (num > dancesNoLongerNeeded) return red;
        else if (num > dancesNeeded && num <= dancesNoLongerNeeded) return orange;
        else return green;
    }
}
