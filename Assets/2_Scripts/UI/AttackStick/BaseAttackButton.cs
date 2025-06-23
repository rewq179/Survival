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
    private bool isInteractable = true;

    public virtual void Reset()
    {
        gameObject.SetActive(false);
        skillIcon.sprite = null;
        isInteractable = true;
    }

    public virtual void Init(int skillId)
    {
        SkillData data = DataManager.GetSkillData(skillId);
        skillIcon.sprite = IconManager.Instance.GetSkillIcon(data.name);

        gameObject.SetActive(true);
    }

    public virtual void OnClick()
    {
        if (!isInteractable)
            return;

        if (buttonType == ButtonType.Once)
            isInteractable = false;
    }
}
