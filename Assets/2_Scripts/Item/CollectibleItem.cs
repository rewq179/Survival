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
    private const float BASE_COLLECT_RANGE = 0.5f;
    private const float MAGNET_EFFECT_SPEED_MULTIPLIER = 3f;
    private const float ITEM_SPAWN_RADIUS = 2f; // 아이템 생성 반지름

    public CollectibleType Type => type;
    public float Value => value;

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

    private void Update()
    {
        if (!isCollected)
        {
            UpdateMagnetEffect();
        }
    }

    private void UpdateMagnetEffect()
    {
        Unit playerUnit = GameManager.Instance.PlayerUnit;
        if (playerUnit == null)
            return;

        Vector3 direction = playerUnit.transform.position - transform.position;
        float dist = direction.sqrMagnitude;
        float currMagnetSpeed = GameManager.Instance.rewardMgr.GetMagnetSpeed();
        float curMagnetRange = GameManager.Instance.rewardMgr.GetMagnetRange();

        if (isGolbalMagentEffect)
            currMagnetSpeed *= MAGNET_EFFECT_SPEED_MULTIPLIER;

        if (isGolbalMagentEffect || dist <= curMagnetRange * curMagnetRange)
        {
            if (!isInMagnetRange)
                isInMagnetRange = true;

            // 자석 효과로 플레이어 방향으로 이동
            direction.Normalize();
            transform.position += direction * currMagnetSpeed * Time.deltaTime;

            // 수집 범위에 들어왔는지 체크
            if (dist <= BASE_COLLECT_RANGE * BASE_COLLECT_RANGE)
                Collect();
        }

        else if (!isGolbalMagentEffect)
        {
            isInMagnetRange = false;
        }
    }

    public void Collect()
    {
        isCollected = true;
        GameManager.Instance.rewardMgr.OnItemCollected(this);
    }
}

