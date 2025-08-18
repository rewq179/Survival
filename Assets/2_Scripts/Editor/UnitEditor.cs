using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    private Unit unit;

    // UI 상태 관리
    private bool showBasicInfo = true;
    private bool showStats = true;
    private bool showCombat = true;
    private bool showSkills = true;
    private bool showBuffs = true;
    private bool showAI = true;
    private bool showDebug = true;

    // 스크롤 위치 캐싱
    private Vector2 statsScrollPosition;
    private Vector2 skillsScrollPosition;
    private Vector2 buffsScrollPosition;

    // 디버그 옵션
    private bool showGizmos = true;
    private bool showColliders = true;
    private bool showAttackRange = true;

    // 상수 정의
    private const float SCROLL_VIEW_HEIGHT = 150f;
    private const float SKILL_NAME_WIDTH = 120f;
    private const float SKILL_LEVEL_WIDTH = 40f;
    private const float SKILL_STATUS_WIDTH = 100f;
    private const float BUFF_NAME_WIDTH = 120f;
    private const float BUFF_STACK_WIDTH = 60f;
    private const float BUFF_DURATION_WIDTH = 80f;

    private void OnEnable()
    {
        unit = (Unit)target;
    }

    public override void OnInspectorGUI()
    {
        if (unit == null)
        {
            EditorGUILayout.HelpBox("유닛이 선택되지 않았습니다.", MessageType.Warning);
            return;
        }

        DrawBasicInfo();
        DrawStats();
        DrawCombat();
        DrawSkills();
        DrawBuffs();
        DrawAI();
        DrawDebug();
        DrawCustomButtons();

        // 변경사항이 있을 때마다 씬 뷰 업데이트
        if (GUI.changed)
        {
            EditorUtility.SetDirty(unit);
            SceneView.RepaintAll();
        }
    }

    private void DrawBasicInfo()
    {
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "기본 정보", true);
        if (!showBasicInfo)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 유닛 타입과 ID
        EditorGUILayout.LabelField("유닛 타입", unit.UnitType.ToString());
        EditorGUILayout.LabelField("유닛 ID", unit.UnitID.ToString());
        EditorGUILayout.LabelField("고유 ID", unit.UniqueID.ToString());

        // 플레이어 여부
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Toggle("플레이어", unit.IsPlayer);
        EditorGUI.EndDisabledGroup();

        // 위치 정보
        EditorGUILayout.LabelField("위치", unit.transform.position.ToString());
        EditorGUILayout.LabelField("회전", unit.transform.rotation.eulerAngles.ToString());

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawStats()
    {
        showStats = EditorGUILayout.Foldout(showStats, "스탯 정보", true);
        if (!showStats)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 기본 스탯
        EditorGUILayout.LabelField("체력", $"{unit.CurHp:F1} / {unit.MaxHp:F1}");
        EditorGUILayout.LabelField("이동속도", $"{unit.MoveSpeed:F2}");

        // 모든 스탯 표시
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("상세 스탯", EditorStyles.boldLabel);

        statsScrollPosition = EditorGUILayout.BeginScrollView(statsScrollPosition, GUILayout.Height(SCROLL_VIEW_HEIGHT));

        for (StatType statType = 0; statType < StatType.Max; statType++)
        {
            float statValue = unit.GetFinalStat(statType);
            EditorGUILayout.LabelField(statType.ToString(), $"{statValue:F2}");
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawCombat()
    {
        showCombat = EditorGUILayout.Foldout(showCombat, "전투 정보", true);
        if (!showCombat)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 전투 상태
        EditorGUILayout.LabelField("사망 여부", unit.IsDead ? "사망" : "생존");
        EditorGUILayout.LabelField("행동 가능", unit.IsActionable ? "가능" : "불가능");
        EditorGUILayout.LabelField("이동 가능", unit.CanMove ? "가능" : "불가능");
        EditorGUILayout.LabelField("공격 가능", unit.CanAttack ? "가능" : "불가능");

        // 레벨 정보
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("레벨 정보", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("레벨", unit.Level.ToString());
        EditorGUILayout.LabelField("경험치", $"{unit.CurExp:F0} / {unit.MaxExp:F0}");
        EditorGUILayout.LabelField("골드", unit.Gold.ToString());

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawSkills()
    {
        showSkills = EditorGUILayout.Foldout(showSkills, "스킬 정보", true);
        if (!showSkills)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 스킬 개수
        List<SkillKey> skills = unit.SkillModule.GetAllSkills();
        EditorGUILayout.LabelField("보유 스킬 수", skills.Count.ToString());

        if (skills.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("보유 스킬", EditorStyles.boldLabel);

            skillsScrollPosition = EditorGUILayout.BeginScrollView(skillsScrollPosition, GUILayout.Height(SCROLL_VIEW_HEIGHT));

            DrawSkillList(skills);

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawSkillList(List<SkillKey> skills)
    {
        foreach (SkillKey skill in skills)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 스킬 이름과 레벨
            EditorGUILayout.LabelField(skill.ToString(), GUILayout.Width(SKILL_NAME_WIDTH));

            int level = unit.SkillModule.GetSkillLevel(skill);
            EditorGUILayout.LabelField($"Lv.{level}", GUILayout.Width(SKILL_LEVEL_WIDTH));

            // 쿨다운 정보
            if (unit.SkillModule.CanUseSkill(skill))
            {
                EditorGUILayout.LabelField("사용 가능", GUILayout.Width(SKILL_STATUS_WIDTH));
            }
            else
            {
                float cooldown = unit.SkillModule.GetSkillCooldown(skill);
                string cooldownText = $"쿨다운: {cooldown:F1}s";
                EditorGUILayout.LabelField(cooldownText, GUILayout.Width(SKILL_STATUS_WIDTH));
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBuffs()
    {
        if (unit.BuffModule == null)
            return;

        showBuffs = EditorGUILayout.Foldout(showBuffs, "버프 정보", true);
        if (!showBuffs)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 활성 버프 개수
        List<BuffInstance> activeBuffs = unit.GetActiveBuffInstances();
        EditorGUILayout.LabelField("활성 버프 수", activeBuffs.Count.ToString());

        if (activeBuffs.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("활성 버프", EditorStyles.boldLabel);

            buffsScrollPosition = EditorGUILayout.BeginScrollView(buffsScrollPosition, GUILayout.Height(SCROLL_VIEW_HEIGHT));

            DrawBuffList(activeBuffs);

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawBuffList(List<BuffInstance> activeBuffs)
    {
        foreach (BuffInstance inst in activeBuffs)
        {
            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.LabelField(inst.buffKey.ToString(), GUILayout.Width(BUFF_NAME_WIDTH));
            EditorGUILayout.LabelField($"스택: {inst.stack}", GUILayout.Width(BUFF_STACK_WIDTH));

            float remainingTime = inst.duration - inst.durationTime;
            EditorGUILayout.LabelField($"지속: {remainingTime:F1}s", GUILayout.Width(BUFF_DURATION_WIDTH));

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawAI()
    {
        showAI = EditorGUILayout.Foldout(showAI, "AI 상태", true);
        if (!showAI)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // AI 상태
        if (!unit.IsPlayer)
        {
            DrawMonsterAI();
        }
        else
        {
            DrawPlayerAI();
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawMonsterAI()
    {
        // 몬스터 AI 상태
        EditorGUILayout.LabelField("AI 상태", "몬스터");

        // 공격 상태
        EditorGUILayout.LabelField("공격 중", unit.IsAttacking ? "예" : "아니오");

        // 강제 이동 상태
        EditorGUILayout.LabelField("강제 이동", unit.IsForceMoving ? "예" : "아니오");

        // 타겟 정보 (플레이어)
        DrawPlayerDistance();
    }

    private void DrawPlayerAI()
    {
        // 플레이어 AI 상태
        EditorGUILayout.LabelField("AI 상태", "플레이어");

        bool isAutoAttack = unit.SkillModule.IsAutoAttackEnabled;
        EditorGUILayout.LabelField("자동 공격", isAutoAttack ? "활성화" : "비활성화");
    }

    private void DrawPlayerDistance()
    {
        Vector3 playerPos = GameMgr.Instance.PlayerUnit.transform.position;
        float distance = Vector3.Distance(unit.transform.position, playerPos);
        EditorGUILayout.LabelField("플레이어와의 거리", $"{distance:F2}");
    }

    private void DrawDebug()
    {
        showDebug = EditorGUILayout.Foldout(showDebug, "디버그 옵션", true);
        if (!showDebug)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("box");

        // 기즈모 표시 옵션
        showGizmos = EditorGUILayout.Toggle("기즈모 표시", showGizmos);
        showColliders = EditorGUILayout.Toggle("콜라이더 표시", showColliders);
        showAttackRange = EditorGUILayout.Toggle("공격 범위 표시", showAttackRange);

        // 실시간 업데이트
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("실시간 정보", EditorStyles.boldLabel);

            if (GUILayout.Button("정보 새로고침"))
            {
                Repaint();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private void DrawCustomButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("커스텀 액션", EditorStyles.boldLabel);

        // 플레이 중일 때만 사용 가능한 버튼들
        if (Application.isPlaying)
        {
            DrawActionButtons();
        }

        else
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능합니다.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("체력 회복(50%)"))
        {
            unit.TakeHeal(unit.MaxHp * 0.5f);
        }

        if (GUILayout.Button("데미지 테스트(-10)"))
        {
            unit.TakeDamage(10f);
        }

        EditorGUILayout.EndHorizontal();

        // 몬스터인 경우 추가 버튼
        if (!unit.IsPlayer && GUILayout.Button("AI 상태 초기화"))
        {
            unit.SetAIState(AIState.Chasing);
        }
    }

    private void OnSceneGUI()
    {
        if (!showGizmos || unit == null)
            return;

        DrawAttackRangeGizmos();
    }

    private void DrawAttackRangeGizmos()
    {
        if (!showAttackRange)
            return;

        Handles.color = Color.red;
        Handles.DrawWireDisc(unit.transform.position, Vector3.up, 1.5f); // 근접 공격 범위
        Handles.DrawWireDisc(unit.transform.position, Vector3.up, 6f);   // 원거리 공격 범위
    }
}