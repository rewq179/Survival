using UnityEngine;
using UnityEngine.UI;

public class BaseAttackButton : MonoBehaviour
{
    protected enum ButtonType
    {
        Continuous,
        Once,
        Cool,
    }

    [Header("UI Components")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private ButtonType buttonType;
    [SerializeField] protected Button attackButton;

    protected Unit playerUnit;
    protected SkillData skillData;

    public virtual void Reset()
    {
        gameObject.SetActive(false);
        skillIcon.sprite = null;
        attackButton.interactable = true;
        playerUnit = null;
        skillData = null;
    }

    public virtual void Init(Unit unit, SkillKey skillKey)
    {
        playerUnit = unit;
        skillData = DataMgr.GetSkillData(skillKey);
        skillIcon.sprite = GameManager.Instance.resourceMgr.GetSkillIcon(skillKey);
        gameObject.SetActive(true);
    }

    public virtual void OnClick()
    {
        if (!attackButton.interactable)
            return;

        switch (buttonType)
        {
            case ButtonType.Once:
                attackButton.interactable = false;
                break;
                
            case ButtonType.Cool:
                attackButton.interactable = false;
                break;
        }
    }
}
