using UnityEngine;

public enum CollectibleType
{
    Gold,
    Exp,
    Magnet,
    Freeze,
    Explosion,
    Heal,
    Max,
}

public class CollectibleItem : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] protected SphereCollider collectCollider;

    // 데이터
    private CollectibleType type;
    private float value;
    private bool isCollected;
    private bool isInMagnetRange;
    /// <summary> 전역 자석 효과 여부 </summary>
    private bool isGolbalMagentEffect;

    // 상수
    public const float BASE_COLLECT_RANGE = 0.5f;
    public const float MAGNET_EFFECT_SPEED_MULTIPLIER = 3f;
    public const float ITEM_SPAWN_RADIUS = 2f; // 아이템 생성 반지름

    public CollectibleType Type => type;
    public float Value => value;
    public bool IsCollected => isCollected;
    public bool IsInMagnetRange => isInMagnetRange;
    public bool IsGolbalMagentEffect => isGolbalMagentEffect;

    public void Reset()
    {
        isCollected = false;
        isInMagnetRange = false;
        isGolbalMagentEffect = false;
        gameObject.SetActive(false);
    }

    public void Init(CollectibleType type, Vector3 position, float value)
    {
        this.type = type;
        this.value = value;
        gameObject.SetActive(true);

        SetRandomPosition(position);
        SetCollider();
    }

    private void SetRandomPosition(Vector3 position)
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * ITEM_SPAWN_RADIUS;
        transform.position = position + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void SetCollider()
    {
        collectCollider.isTrigger = true;
        collectCollider.radius = BASE_COLLECT_RANGE;
    }

    public void ApplyGlobalMagnetEffect()
    {
        isGolbalMagentEffect = true;
        isInMagnetRange = true;
    }

    public void UpdatePosition(Vector3 pos)
    {
        transform.position = pos;
    }
    
    public bool IsInCollectRange(float distanceSqr)
    {
        return distanceSqr <= BASE_COLLECT_RANGE * BASE_COLLECT_RANGE;
    }
    
    public void SetGlobalMagnetEffect(bool isGlobal)
    {
        isGolbalMagentEffect = isGlobal;
    }

    public void Collect()
    {
        isCollected = true;
        GameMgr.Instance.rewardMgr.OnItemCollected(this);
    }
}

