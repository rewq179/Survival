using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 사각형 내 아이콘과 텍스트가 포함된 슬롯
/// </summary>
public class IconSlot : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject gradientObject;
    [SerializeField] private TextMeshProUGUI countText;

    public void Reset()
    {
        iconImage.sprite = null;
        countText.text = string.Empty;
        gradientObject.SetActive(false);
    }

    public void Init(Sprite icon, string text)
    {
        iconImage.sprite = icon;

        if (text != string.Empty)
        {
            countText.text = text;
            gradientObject.SetActive(true);
        }
    }
}
