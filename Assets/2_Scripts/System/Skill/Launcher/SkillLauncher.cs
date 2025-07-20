using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkillLauncher : MonoBehaviour
{
    private SkillKey skillKey;
    private Vector3 startPosition;
    private Vector3 direction;
    private Unit caster;
    private bool isActive;
    private float duration;
    private float time;
    private bool isParticleFinished;

    // 컴포넌트 관리
    private bool isCurrentOrderCompleted;  // 현재 Order 완료 여부
    private int currentOrderIndex;  // 현재 실행 중인 Order
    private List<int> orderSequence = new();  // Order 실행 순서
    private Dictionary<int, List<SkillComponent>> orderGroups = new();  // Order별 그룹

    // 현재 Order 내 컴포넌트 실행 관리
    private int currentSequentialIndex;  // 현재 순차 실행 중인 컴포넌트 인덱스
    private bool isSequentialExecutionComplete;  // 순차 실행 완료 여부
    private List<SkillComponent> immediateComponents = new();  // 현재 Order의 즉시 실행 컴포넌트들
    private List<SkillComponent> sequentialComponents = new();  // 현재 Order의 순차 실행 컴포넌트들

    public SkillKey SkillKey => skillKey;
    public bool IsActive => isActive;
    public Unit Caster => caster;
    public Vector3 Position => transform.position;
    public Vector3 Direction => direction;

    public void Reset()
    {
        isActive = false;
        time = 0f;
        orderGroups.Clear();
        orderSequence.Clear();
        currentOrderIndex = 0;
        isCurrentOrderCompleted = false;
        immediateComponents.Clear();
        sequentialComponents.Clear();
        currentSequentialIndex = 0;
        isSequentialExecutionComplete = false;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isActive)
            return;

        time += Time.deltaTime;
        UpdateComponentsByOrder();
    }

    public void Init(SkillInstance inst, Vector3 startPos, Vector3 dir, Unit caster, Unit fixedTarget = null)
    {
        skillKey = inst.skillKey;
        isParticleFinished = true;
        Init(caster, startPos, dir);

        // 스킬 데이터 기반으로 효과들 자동 추가
        CreateComponentBySkill(inst, fixedTarget);
        StartOrderComponents();
    }

    public void Init(Unit caster, Vector3 position, Vector3 direction)
    {
        this.caster = caster;
        startPosition = position;
        this.direction = direction.normalized;
        SetTransform(position, direction);
        isActive = true;
        time = 0f;

        gameObject.SetActive(true);
    }

    public void SetTransform(Vector3 startPos, Vector3 dir)
    {
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetParticleFinished(bool isFinished)
    {
        isParticleFinished = isFinished;
    }

    private void CreateComponentBySkill(SkillInstance inst, Unit fixedTarget)
    {
        // 컴포넌트 생성 및 Order별 그룹화
        List<InstanceValue> values = inst.Values;
        foreach (InstanceValue instValue in values)
        {
            SkillComponent component = CreateComponent(instValue.type, instValue.order);
            if (component == null)
                continue;

            component.Init(this, instValue, fixedTarget);
        }

        orderSequence = orderGroups.Keys.OrderBy(x => x).ToList();
    }

    public void CreateComponentByCollectible(CollectibleType type)
    {
        SkillComponentType componentType = type switch
        {
            CollectibleType.Freeze => SkillComponentType.Freeze,
            CollectibleType.Explosion => SkillComponentType.Explosion,
            _ => SkillComponentType.Max,
        };

        if (componentType == SkillComponentType.Max)
            return;

        CreateComponent(componentType, 0).Init(this, null, null);
    }

    private SkillComponent CreateComponent(SkillComponentType type, int order)
    {
        SkillComponent component = GameMgr.Instance.skillMgr.PopComponent(type);
        if (component == null)
            return null;

        if (!orderGroups.ContainsKey(order))
            orderGroups[order] = new List<SkillComponent>();

        orderGroups[order].Add(component);
        return component;
    }

    /// <summary>
    /// 해당 Order의 컴포넌트들을 시작함.
    /// </summary>
    private void StartOrderComponents()
    {
        if (currentOrderIndex >= orderSequence.Count)
            return;

        int order = orderSequence[currentOrderIndex];
        List<SkillComponent> components = orderGroups[order];

        // 현재 Order의 컴포넌트들을 즉시/순차로 분류만 수행
        immediateComponents.Clear();
        sequentialComponents.Clear();

        foreach (SkillComponent component in components)
        {
            if (component.timing == ExecutionTiming.Immediate)
            {
                immediateComponents.Add(component);
            }

            else if (component.timing == ExecutionTiming.Sequential)
            {
                sequentialComponents.Add(component);
            }
        }

        // 순차 실행 초기화
        currentSequentialIndex = 0;
        isSequentialExecutionComplete = sequentialComponents.Count == 0;
    }

    private void UpdateComponentsByOrder()
    {
        if (currentOrderIndex >= orderSequence.Count)
            return;

        // 현재 Order의 즉시 실행 컴포넌트들 시작 및 업데이트
        bool allImmediateCompleted = true;
        foreach (SkillComponent component in immediateComponents)
        {
            if (component.State == ComponentState.NotStarted)
            {
                component.OnStart(this);
            }

            if (component.State == ComponentState.Running)
            {
                component.OnUpdate(Time.deltaTime);

                if (!component.IsCompleted) // 진행중에 종료된 경우
                    allImmediateCompleted = false;
            }
        }

        // 즉시 실행 컴포넌트들이 모두 완료되었고, 순차 실행이 아직 완료되지 않았다면
        if (allImmediateCompleted && !isSequentialExecutionComplete)
        {
            UpdateSequentialComponents();
        }

        // 현재 Order의 모든 컴포넌트가 완료되었는지 체크
        bool allSequentialCompleted = true;
        foreach (SkillComponent component in sequentialComponents)
        {
            if (component.State != ComponentState.Completed)
            {
                allSequentialCompleted = false;
                break;
            }
        }

        // 현재 Order 완료 시 다음 Order로 이동
        if (allImmediateCompleted && allSequentialCompleted && !isCurrentOrderCompleted)
        {
            isCurrentOrderCompleted = true;
            if (++currentOrderIndex < orderSequence.Count)
                return;

            // 다음 Order의 컴포넌트들 시작
            StartOrderComponents();
        }
    }

    private void UpdateSequentialComponents()
    {
        // 모든 순차 컴포넌트가 완료되었는지 체크
        if (currentSequentialIndex >= sequentialComponents.Count)
        {
            isSequentialExecutionComplete = true;
            return;
        }

        // 현재 순차 컴포넌트 가져오기
        SkillComponent currentComponent = sequentialComponents[currentSequentialIndex];
        switch (currentComponent.State)
        {
            case ComponentState.NotStarted:
                currentComponent.OnStart(this);
                break;
            case ComponentState.Running:
                currentComponent.OnUpdate(Time.deltaTime);
                break;
            case ComponentState.Completed:
                currentSequentialIndex++;
                break;
        }
    }

    public void CheckDeactivate()
    {
        if (!isActive || !isParticleFinished)
            return;

        if (currentOrderIndex >= orderSequence.Count)
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        if (!isActive)
            return;

        isActive = false;

        SkillMgr skillMgr = GameMgr.Instance.skillMgr;
        foreach (var group in orderGroups)
        {
            List<SkillComponent> components = group.Value;
            foreach (SkillComponent component in components)
            {
                skillMgr.PushComponent(component);
            }
        }

        skillMgr.RemoveLauncher(this);
    }
}