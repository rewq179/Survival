using UnityEngine;

[System.Serializable]
public class SelectionData
{
    public SelectionSlotType type;
    public SkillKey skillKey;
    public string name;
    public string description;
    public Sprite icon;
    public bool isLevelUp;
    
    public void Init(SkillKey skillKey, string name, string description, Sprite icon, bool isLevelUp = false)
    {
        this.skillKey = skillKey;
        this.name = name;
        this.description = description;
        this.icon = icon;
        this.isLevelUp = isLevelUp;

        type = skillKey != SkillKey.None ? SelectionSlotType.Skill : SelectionSlotType.Item;
    }
}