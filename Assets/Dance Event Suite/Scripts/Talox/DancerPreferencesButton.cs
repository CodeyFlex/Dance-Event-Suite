
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DancerPreferencesButton : UdonSharpBehaviour
{
    [SerializeField] private DancerPreferences dancerPreferences;
    [SerializeField] private TMP_Text preferenceNameText;
    
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color EnabledColor = Color.green;
    [SerializeField] private Color DisabledColor = Color.white;
    private int index;
    
    public void Initialize(int i, string preferenceName)
    {
        index = i;
        preferenceNameText.text = preferenceName;
    }

    public void OnClick()
    {
        dancerPreferences.SetRank(index);
    }


    public void SetValue(bool p0)
    {
        buttonImage.color = p0 ? EnabledColor : DisabledColor;
    }
}
