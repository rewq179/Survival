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
    private Unit unit;
    private SkillManager skillManager;

    public void Init(Unit unit)
    {
        foreach (var button in attackButtons)
        {
            button.Reset();
        }

        this.unit = unit;
        skillManager = GameManager.Instance.skillManager;

        UnsubscribeFromEvents();
        SubscribeToEvents();

        panel.SetActive(true);
    }

    private void UnsubscribeFromEvents()
    {
        if (skillManager != null)
        {
            skillManager.OnSkillListChanged -= RefreshSkill;
        }
    }

    private void SubscribeToEvents()
    {
        if (skillManager != null)
        {
            skillManager.OnSkillListChanged += RefreshSkill;
        }
    }

    private void RefreshSkill(Dictionary<SkillKey, List<SkillKey>> skillKeys)
    {
        int start = (int)SkillButtonType.Attack;

        int index = 0;
        foreach (var skill in skillKeys)
        {
            if (!DataMgr.IsActiveSkill(skill.Key))
                continue;
                
            attackButtons[start + index].Init(skill.Key);
            index++;
        }
    }
}
