using UnityEngine;

public enum SkillEffectType
{
    MagicCircle,
    Explosion,
    Beam
}

public class SkillEffectManager : MonoBehaviour
{
    [Header("Effect Prefabs")]
    [SerializeField] private GameObject magicCirclePrefab;
    [SerializeField] private GameObject explosionParticlePrefab;
    [SerializeField] private GameObject beamEffectPrefab;
    
    public void PlaySkillEffect(int skillId, Vector3 position, Vector3 direction)
    {
        SkillData skillData = DataManager.GetSkillData(skillId);
        
        // switch (skillData.effectType)
        // {
        //     case SkillEffectType.MagicCircle:
        //         SpawnMagicCircle(position, skillData);
        //         break;
                
        //     case SkillEffectType.Explosion:
        //         SpawnExplosionEffect(position, skillData);
        //         break;
                
        //     case SkillEffectType.Beam:
        //         SpawnBeamEffect(position, direction, skillData);
        //         break;
        // }
    }
    
    private void SpawnMagicCircle(Vector3 position, SkillData skillData)
    {
        // GameObject effect = Instantiate(magicCirclePrefab, position, Quaternion.identity);
        // effect.transform.localScale = Vector3.one * skillData.range;
        
        // 자동 제거
        // Destroy(effect, skillData.duration);
    }
}