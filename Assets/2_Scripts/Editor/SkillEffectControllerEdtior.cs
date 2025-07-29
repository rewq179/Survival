#if UNITY_EDITOR
using System.Net;
using System.Runtime.Remoting.Metadata;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillEffectController))]
public class SkillEffectControllerEditor : Editor
{
    private ParticleSystem measureTarget;
    private Renderer measureTargetRenderer;

    private SkillEffectController controller;
    private SkillCollision collision;
    private SphereCollider sphereCollider;
    private string measureTargetKey;
    private float maxRendererRadius; // 저장된 maxRendererRadius를 필드로 추가

    private void OnEnable()
    {
        controller = (SkillEffectController)target;
        collision = controller.GetComponent<SkillCollision>();
        if (collision != null)
        {
            sphereCollider = collision.Collider as SphereCollider;
        }

        // 각 인스턴스별 고유 키 생성
        measureTargetKey = $"SkillEffectController_MeasureTarget_{controller.GetInstanceID()}";

        // 저장된 measureTarget 복원
        LoadMeasureTarget();
        LoadMaxRendererRadius(); // 저장된 maxRendererRadius 복원
    }

    private void LoadMeasureTarget()
    {
        string savedID = EditorPrefs.GetString(measureTargetKey, "");
        if (string.IsNullOrEmpty(savedID))
            return;

        if (int.TryParse(savedID, out int instanceID))
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
                return;

            measureTarget = go.GetComponent<ParticleSystem>();
            measureTargetRenderer = go.GetComponent<Renderer>();
        }
    }

    private void SaveMeasureTarget()
    {
        if (measureTarget != null)
        {
            int instanceID = measureTarget.gameObject.GetInstanceID();
            EditorPrefs.SetString(measureTargetKey, instanceID.ToString());
        }

        else
        {
            EditorPrefs.DeleteKey(measureTargetKey);
        }
    }

    private void LoadMaxRendererRadius()
    {
        string savedRadius = EditorPrefs.GetString($"SkillEffectController_MaxRendererRadius_{controller.GetInstanceID()}", "0");
        if (float.TryParse(savedRadius, out float radius))
        {
            maxRendererRadius = radius;
        }
    }

    private void SaveMaxRendererRadius()
    {
        EditorPrefs.SetString($"SkillEffectController_MaxRendererRadius_{controller.GetInstanceID()}", maxRendererRadius.ToString());
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("에디터 ~ 크기 매칭", EditorStyles.boldLabel);

        // 측정 대상 파티클 선택
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("측정 대상", EditorStyles.boldLabel);
        ParticleSystem newMeasureTarget = (ParticleSystem)EditorGUILayout.ObjectField("대상 파티클", measureTarget, typeof(ParticleSystem), true);

        // 값이 변경되면 저장
        if (newMeasureTarget != measureTarget)
        {
            measureTarget = newMeasureTarget;
            measureTargetRenderer = measureTarget.GetComponent<Renderer>();
            SaveMeasureTarget();

            // 새로운 파티클이 선택되면 maxRendererRadius도 업데이트
            maxRendererRadius = measureTargetRenderer.bounds.extents.magnitude;
            SaveMaxRendererRadius();
        }
        EditorGUILayout.EndVertical();

        // 현재 상태 표시
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("현재 상태:", EditorStyles.boldLabel);

        if (sphereCollider != null)
        {
            EditorGUILayout.LabelField($"콜라이더 반지름: {sphereCollider.radius:F3}");
        }

        EditorGUILayout.LabelField($"파티클 Renderer bounds: {maxRendererRadius:F3}");

        float particleRadius = GetParticleRadius();
        EditorGUILayout.LabelField($"최대 파티클 반지름: {particleRadius:F3}");

        if (sphereCollider != null)
        {
            float difference = Mathf.Abs(sphereCollider.radius - particleRadius);
            EditorGUILayout.LabelField($"차이: {difference:F3}");

            if (difference > 0.1f)
            {
                EditorGUILayout.HelpBox("파티클과 콜라이더 크기가 다릅니다!", MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // 도구 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("콜라이더 크기 조정"))
        {
            AdjustColliderSize();
        }

        if (GUILayout.Button("저장된 대상 초기화"))
        {
            measureTarget = null;
            maxRendererRadius = 0f;
            SaveMeasureTarget();
            SaveMaxRendererRadius();
        }
        EditorGUILayout.EndHorizontal();
    }

    private float GetParticleRadius()
    {
        if (measureTarget == null)
            return 0f;

        Renderer renderer = measureTarget.GetComponent<Renderer>();
        if (renderer == null)
            return 1f;

        float radius = renderer.bounds.extents.magnitude;
        return radius > 0.01f ? radius : 1f;
    }

    private void AdjustColliderSize()
    {
        if (sphereCollider == null)
        {
            EditorUtility.DisplayDialog("오류", "SphereCollider가 없습니다!", "확인");
            return;
        }

        float particleRadius = GetParticleRadius();
        sphereCollider.radius = particleRadius;

        EditorUtility.SetDirty(sphereCollider);
        EditorUtility.SetDirty(controller);
        EditorUtility.DisplayDialog("조정", $"콜라이더 반지름을 {particleRadius:F3}로 조정했습니다.", "확인");
    }
}
#endif