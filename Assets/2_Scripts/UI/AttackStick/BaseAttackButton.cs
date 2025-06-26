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

    [SerializeField] private Image skillIcon;
    [SerializeField] private ButtonType buttonType;
    [SerializeField] protected Button attackButton;
    protected SkillData skillData;

    public virtual void Reset()
    {
        gameObject.SetActive(false);
        skillIcon.sprite = null;
        attackButton.interactable = true;
        skillData = null;
    }

    public virtual void Init(SkillKey skillKey)
    {
        skillData = DataManager.GetSkillData(skillKey);
        skillIcon.sprite = GameManager.Instance.resourceManager.GetSkillIcon(skillData.name);
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
