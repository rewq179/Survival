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
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button clickButton;

    [Header("Utility Components")]
    [SerializeField] private IconSlot iconSlot;
    [SerializeField] private DetailInfo detailInfo;

    // 상수 정의
    private const string ACTIVE_SKILL_TEXT = "액티브";
    private const string PASSIVE_SKILL_TEXT = "패시브";
    private const string SUB_SKILL_TEXT = "서브";
    private const string ITEM_TEXT = "아이템";

    // 데이터
    private SelectionData data;
    private bool isSelected;
    private System.Action<SelectionData> onSlotClicked;

    public RectTransform Rect => rect;
    public SelectionSlotType Type => data.type;

    public void Reset()
    {
        data = null;
        onSlotClicked = null;
        detailInfo.Reset();
        iconSlot.Reset();
        clickButton.onClick.RemoveAllListeners();
        isSelected = false;
    }

    public void Init(SelectionData data, System.Action<SelectionData> onClickCallback)
    {
        this.data = data;
        this.onSlotClicked = onClickCallback;
        UpdateUI();
        clickButton.onClick.AddListener(OnClick);
    }

    public void UpdatePosition(Vector2 position)
    {
        rect.anchoredPosition = position;
    }

    private void UpdateUI()
    {
        if (data == null)
            return;

        titleText.text = GetSkillTypeText(data.skillType);
        iconSlot.Init(data.icon, string.Empty);
        nameText.text = data.name;
        descText.text = SkillDescriptionGenerator.GetSlectionSlotDesc(data);

        detailInfo.Init(data.name, descText.text);
    }

    private string GetSkillTypeText(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Active => ACTIVE_SKILL_TEXT,
            SkillType.Passive => PASSIVE_SKILL_TEXT,
            SkillType.Sub => SUB_SKILL_TEXT,
            _ => ITEM_TEXT
        };
    }

    public void OnClick()
    {
        if (isSelected)
            return;

        isSelected = true;
        onSlotClicked(data);
    }
}
