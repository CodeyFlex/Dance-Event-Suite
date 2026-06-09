
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class RankButton : UdonSharpBehaviour
{
    private int rankIndex;
    
    [SerializeField]
    private DancerRank dancerRank;
    [SerializeField]
    private TMPro.TMP_Text rankNameText;
    [SerializeField]
    private Image backgroundImage;
    
    [SerializeField]
    private Color selectedColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    [SerializeField]
    private Color unselectedColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    
    public void Initialize(int index,string rankName)
    {
        rankIndex = index;
        rankNameText.text = rankName;
    }
    
    public void OnClick()
    {
        dancerRank.SetRank(rankIndex);
    }
    
    public void SetSelected(bool isSelected)
    {
        backgroundImage.color = isSelected ?selectedColor : unselectedColor;
    }
}
