using UnityEngine;

[System.Serializable]
public class SelectionData
{
    public SelectionSlotType type;
    public SkillKey skillKey;
    public SkillType skillType;
    public string name;
    public string description;
    public Sprite icon;
    
    public void Init(SkillKey skillKey, SkillType skillType, string name, string description, Sprite icon)
    {
        this.skillKey = skillKey;
        this.skillType = skillType;
        this.name = name;
        this.description = description;
        this.icon = icon;

        type = skillKey != SkillKey.None ? SelectionSlotType.Skill : SelectionSlotType.Item;
    }
}