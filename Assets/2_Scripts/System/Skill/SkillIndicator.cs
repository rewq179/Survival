using UnityEngine;
using System.Collections.Generic;

public enum SkillIndicatorType
{
    Line,
    Rectangle,
    Sector,
    Circle,
    InstantAttack,
    Max,
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkillIndicator : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    private Mesh mesh;
    private SkillElement skillElement;
    private bool isMainIndicator;
    public bool isPlayerIndicator;

    private const float SEGMENTS = 32;      // 원/부채꼴 세분화
    private const float INV_SEGMENTS = 1 / SEGMENTS;
    private Vector3 originalScale = Vector3.one;

    public SkillElement Element => skillElement;
    public Mesh Mesh => mesh;

    public void Init(SkillElement element, Material indicatorMaterial, Mesh mesh, bool isPlayerIndicator)
    {
        skillElement = element;
        this.mesh = mesh;
        isMainIndicator = skillElement.IsMainIndicator;
        this.isPlayerIndicator = isPlayerIndicator;

        meshRenderer.material = indicatorMaterial;
        gameObject.SetActive(true);
    }

    public void SetMaxSize() => transform.localScale = originalScale;
    public void SetMinSize() => transform.localScale = Vector3.zero;

    public void Reset()
    {
        if (meshFilter.mesh != null)
            meshFilter.mesh = null;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        mesh = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 직선 메시 생성
    /// </summary>
    /// <param name="length">메시 길이</param>
    /// <param name="width">메시 너비</param>
    public static Mesh CreateLineMesh(float length, float width)
    {
        Mesh mesh = new Mesh();
        float halfWidth = width * 0.5f;
        Vector3[] vertices = new Vector3[]
        {
             new Vector3(-halfWidth, 0, 0),
            new Vector3(halfWidth, 0, 0),
            new Vector3(-halfWidth, 0, length),
            new Vector3(halfWidth, 0, length)
        };

        int[] triangles = new int[]
        {
            0, 2, 1,
            1, 2, 3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// 부채꼴 메시 생성
    /// </summary>
    /// <param name="angle">부채꼴 각도</param>
    /// <param name="radius">부채꼴 반지름</param>
    public static Mesh CreateSectorMesh(float angle, float radius)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3> { Vector3.zero };
        List<int> triangles = new List<int>();

        float angleStep = angle * INV_SEGMENTS;
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float currentAngle = -angle * 0.5f + angleStep * i;
            float rad = Mathf.Deg2Rad * currentAngle;
            vertices.Add(new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius));
        }

        for (int i = 1; i <= SEGMENTS; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// 원형 메시 생성
    /// </summary>
    /// <param name="radius">원형 반지름</param>
    public static Mesh CreateCircleMesh(float radius)
    {
        return CreateSectorMesh(360f, radius);
    }

    public void DrawIndicator(Vector3 start, Vector3 mouse)
    {
        if (isMainIndicator)
        {
            switch (skillElement.indicatorType)
            {
                case SkillIndicatorType.Line:
                case SkillIndicatorType.Rectangle:
                    transform.position = start;
                    Vector3 lineDir = (mouse - start).normalized;
                    transform.rotation = Quaternion.LookRotation(lineDir);
                    break;

                case SkillIndicatorType.Sector:
                    transform.position = start;
                    Vector3 sectorDir = (mouse - start).normalized;
                    transform.rotation = Quaternion.LookRotation(sectorDir);
                    break;

                case SkillIndicatorType.Circle:
                    transform.position = GetElementEndPoint(start, mouse, skillElement);
                    transform.rotation = Quaternion.identity;
                    break;
            }
        }

        else
        {
            switch (skillElement.indicatorType)
            {
                case SkillIndicatorType.Line:
                    transform.position = start;
                    transform.rotation = Quaternion.identity;
                    break;

                case SkillIndicatorType.Sector:
                    transform.position = start;
                    transform.rotation = Quaternion.identity;
                    break;

                case SkillIndicatorType.Circle:
                    transform.position = start;
                    transform.rotation = Quaternion.identity;
                    break;
            }
        }

        meshFilter.mesh = mesh;
    }

    public void UpdateIndicatorScale(float p)
    {
        switch (skillElement.indicatorType)
        {

            case SkillIndicatorType.Sector:
                break;

            case SkillIndicatorType.Circle:
                transform.localScale = originalScale * p;
                break;

            case SkillIndicatorType.Rectangle:
                Vector3 scale = originalScale;
                scale.z = p;
                transform.localScale = scale;
                break;
        }
    }

    /// <summary>
    /// 마우스 방향에 따른 최대 길이 계산
    /// </summary>
    public static Vector3 GetElementEndPoint(Vector3 startPoint, Vector3 mouse, SkillElement element)
    {
        if (element == null)
            return startPoint;

        Vector3 direction = (mouse - startPoint).normalized;
        Vector3 basePosition = element.firePoint == FirePoint.Self ? startPoint : mouse;

        switch (element.indicatorType)
        {
            case SkillIndicatorType.Line:
                return basePosition + direction * GameValue.PROJECTILE_MAX_LENGTH;

            case SkillIndicatorType.Sector:
                float halfAngleRad = element.Angle * 0.5f * Mathf.Deg2Rad;
                float sectorDistance = element.Radius * Mathf.Cos(halfAngleRad) * 2f;
                return basePosition + direction * sectorDistance;

            case SkillIndicatorType.Circle:
                if (element.firePoint == FirePoint.Self)
                    return startPoint;
                
                float distanceToMouse = (mouse - startPoint).magnitude;
                if (distanceToMouse > element.maxDistance)
                    return startPoint + direction * element.maxDistance;
                
                return mouse;
        }

        return basePosition;
    }
}