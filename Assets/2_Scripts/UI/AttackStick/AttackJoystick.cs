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

    public void Init(Unit unit)
    {
        foreach (var button in attackButtons)
        {
            button.Reset();
        }

        this.unit = unit;
        UnsubscribeFromEvents();
        SubscribeToEvents();

        panel.SetActive(true);
    }

    private void UnsubscribeFromEvents()
    {
        unit.OnSkillChanged -= RefreshSkill;
    }

    private void SubscribeToEvents()
    {
        unit.OnSkillChanged += RefreshSkill;
    }

    private void RefreshSkill(List<int> skillIds)
    {
        int start = (int)SkillButtonType.Attack;

        for (int i = 0; i < skillIds.Count; i++)
        {
            attackButtons[start + i].Init(skillIds[i]);
        }
    }
}
