using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SelectionSlotType
{
    Skill,
    Item,
}

public class SelectionSlot : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private DetailInfo detailInfo;
    [SerializeField] private Button clickButton;

    // 데이터
    private SelectionData data;
    private System.Action<SelectionData> onSlotClicked;

    public RectTransform Rect => rect;
    public SelectionSlotType Type => data.type;

    public void Reset()
    {
        data = null;
        onSlotClicked = null;
        detailInfo.Reset();
        clickButton.onClick.RemoveAllListeners();
    }

    public void Init(SelectionData data, System.Action<SelectionData> onClickCallback)
    {
        this.data = data;
        UpdateUI();
        onSlotClicked = onClickCallback;
        clickButton.onClick.AddListener(OnClick);
    }

    public void UpdatePosition(Vector2 position)
    {
        rect.anchoredPosition = position;
    }

    private void UpdateUI()
    {
        titleText.text = data.skillType switch
        {
            SkillType.Active => "액티브",
            SkillType.Passive => "패시브",
            SkillType.Sub => "서브",
            _ => "아이템"
        };

        iconImage.sprite = data.icon;
        nameText.text = data.name;

        float value = 0;
        if (data.skillType == SkillType.Passive)
            value = DataMgr.GetSkillData(data.skillKey).baseValue;
        else if (data.skillType == SkillType.Sub)
            value = DataMgr.GetSubSkillData(data.skillKey).baseValue;

        string desc = DescMgr.GetSubSkillDescription(data.description, value);
        if (data.skillType == SkillType.Sub)
            desc += "\n\n<color=yellow>레벨업!</color>";
        else
            desc += "\n\n<color=green>새로 획득!</color>";

        descText.text = desc;
        // detailInfo.Init(data.name, desc);
    }

    public void OnClick()
    {
        onSlotClicked(data);
    }
}
