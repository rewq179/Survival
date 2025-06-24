using UnityEngine;
using System.Collections.Generic;

public enum IconType
{
    Skill,
    Equipment,
    Item
}

public class IconManager : MonoBehaviour
{
    private string skillIconPath = "Skill";
    private string equipmentIconPath = "Equipment";
    private string itemIconPath = "Item";

    private Dictionary<string, Sprite> skillIcons = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> equipmentIcons = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> itemIcons = new Dictionary<string, Sprite>();

    public void LoadAllIcons()
    {
        LoadIconsFromPath(skillIconPath, skillIcons);
        LoadIconsFromPath(equipmentIconPath, equipmentIcons);
        LoadIconsFromPath(itemIconPath, itemIcons);
    }

    private void LoadIconsFromPath(string path, Dictionary<string, Sprite> iconDictionary)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);

        foreach (Sprite sprite in sprites)
        {
            if (sprite != null)
            {
                iconDictionary[sprite.name] = sprite;
            }
        }
    }

    public Sprite GetSkillIcon(string iconName)
    {
        if (skillIcons.TryGetValue(iconName, out Sprite sprite))
            return sprite;

        return GetDefaultIcon();
    }

    public Sprite GetEquipmentIcon(string iconName)
    {
        if (equipmentIcons.TryGetValue(iconName, out Sprite sprite))
        {
            return sprite;
        }

        return GetDefaultIcon();
    }

    public Sprite GetItemIcon(string iconName)
    {
        if (itemIcons.TryGetValue(iconName, out Sprite sprite))
        {
            return sprite;
        }

        return GetDefaultIcon();
    }

    public Sprite GetIconByType(IconType type, string iconName)
    {
        switch (type)
        {
            case IconType.Skill:
                return GetSkillIcon(iconName);
            case IconType.Equipment:
                return GetEquipmentIcon(iconName);
            case IconType.Item:
                return GetItemIcon(iconName);
            default:
                return GetDefaultIcon();
        }
    }

    private Sprite GetDefaultIcon()
    {
        // 기본 아이콘 반환 (Resources 폴더에 DefaultIcon.png 등이 있다면)
        Sprite defaultIcon = Resources.Load<Sprite>("DefaultIcon");
        return defaultIcon;
    }

    public bool HasIcon(IconType type, string iconName)
    {
        switch (type)
        {
            case IconType.Skill:
                return skillIcons.ContainsKey(iconName);
            case IconType.Equipment:
                return equipmentIcons.ContainsKey(iconName);
            case IconType.Item:
                return itemIcons.ContainsKey(iconName);
            default:
                return false;
        }
    }

    public void ClearCache()
    {
        skillIcons.Clear();
        equipmentIcons.Clear();
        itemIcons.Clear();
    }
}