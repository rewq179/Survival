using System.Collections.Generic;
using UnityEngine;

public class AttackJoystick : MonoBehaviour
{
    public enum SkillButtonType
    {
        Dash,
        Attack,
        SpecialAttack,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private BaseAttackButton[] attackButtons;
    private Unit playerUnit;
    private bool isAutoAttack;

    public void Init(Unit unit)
    {
        foreach (var button in attackButtons)
        {
            button.Reset();
        }

        isAutoAttack = false;
        playerUnit = unit;

        UnsubscribeFromEvents();
        SubscribeToEvents();

        panel.SetActive(true);
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.Instance.OnPlayerSkillAdded -= RefreshSkill;
    }

    private void SubscribeToEvents()
    {
        GameEvents.Instance.OnPlayerSkillAdded += RefreshSkill;
    }

    private void RefreshSkill(SkillKey skillKey)
    {
        if (DataMgr.IsPassiveSkill(skillKey))
            return;

        foreach (BaseAttackButton button in attackButtons)
        {
            if (!button.IsSetted)
            {
                button.Init(playerUnit, skillKey, !isAutoAttack);
                break;
            }
        }
    }

    public void SetAutoAttack(bool isAutoAttack)
    {
        this.isAutoAttack = isAutoAttack;

        foreach (BaseAttackButton button in attackButtons)
        {
            if (!button.IsSetted)
                continue;

            button.gameObject.SetActive(!isAutoAttack);
        }
    }
}
