using UnityEngine;
using TMPro;

public class DetailInfo : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;

    public void Reset()
    {
        nameText.text = string.Empty;
        descText.text = string.Empty;
        gameObject.SetActive(false);
    }

    public void Init(string name, string desc)
    {
        nameText.text = name;
        descText.text = desc;
        gameObject.SetActive(true);
    }
}
