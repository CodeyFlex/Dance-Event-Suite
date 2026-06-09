
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LeaderboardObject : UdonSharpBehaviour
{
    [UdonSynced]
    public bool hasJoined;
    [UdonSynced]
    public double servertime;

    [SerializeField]
    private CanvasGroup MainCanvasGroup;
    [SerializeField]
    private Transform parent;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text numberText;
    [SerializeField]
    private TMP_Text timeText;

    public void Start()
    {
        parent = transform.parent;
        nameText.text = Networking.GetOwner(gameObject).displayName;
        transform.SetAsLastSibling();
    }

    public override void OnDeserialization()
    {
        UpdateView();
    }

    private void UpdateView()
    {
        if (hasJoined)
        {
            MainCanvasGroup.alpha = 1;
            transform.SetAsLastSibling();
            int i;
            for (i = 0; i < parent.childCount; i++)
            {
                LeaderboardObject leaderboardObject = parent.GetChild(i).GetComponent<LeaderboardObject>();
                leaderboardObject.UpdateIndex();

                if (!leaderboardObject.hasJoined)
                {
                    transform.SetSiblingIndex(i);
                    break;
                }

                if (leaderboardObject.servertime < servertime)
                {
                    continue;
                }

                transform.SetSiblingIndex(i);
                break;
            }

            UpdateIndex();
            i++;

            for (; i < parent.childCount; i++)
            {
                LeaderboardObject leaderboardObject = parent.GetChild(i).GetComponent<LeaderboardObject>();
                leaderboardObject.UpdateIndex();
            }
        }
        else
        {
            int i = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            for (; i < parent.childCount; i++)
            {
                LeaderboardObject leaderboardObject = parent.GetChild(i).GetComponent<LeaderboardObject>();
                leaderboardObject.UpdateIndex();
            }
            MainCanvasGroup.alpha = 0;
        }
    }

    public void _Leave()
    {
        hasJoined = false;
        RequestSerialization();
        UpdateView();
    }

    public void _Join()
    {
        hasJoined = true;
        servertime = Networking.GetServerTimeInSeconds();
        RequestSerialization();
        UpdateView();
    }

    private void UpdateIndex()
    {
        numberText.text = (transform.GetSiblingIndex() + 1).ToString() + ".";
    }
}
