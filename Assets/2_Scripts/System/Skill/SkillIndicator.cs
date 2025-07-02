using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;

[System.Serializable]
public struct IndicatorElement
{
    public SkillKey skillKey;
    public int index;
    public SkillIndicatorType type;
    public float moveSpeed;
    public float length;
    public float width;
    public float angle;
    public float radius;
    public float maxDistance;

    public bool IsMainIndicator => index == 0;

    public IndicatorElement(SkillKey skillKey, int index, float speed = 0, float length = 0, float width = 0, float angle = 0, float radius = 0)
    {
        this.skillKey = skillKey;
        this.index = index;
        this.moveSpeed = speed;
        this.length = length;
        this.width = width;
        this.angle = angle;
        this.radius = radius;

        if (length > 0)
            type = SkillIndicatorType.Line;
        else if (angle == 0)
            type = SkillIndicatorType.InstantAttack;
        else if (angle < 360)
            type = SkillIndicatorType.Sector;
        else
            type = SkillIndicatorType.Circle;

        maxDistance = radius > 0 ? radius * 2f : width * 1.6f;
    }
}

public enum SkillIndicatorType
{
    Line,
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
    private IndicatorElement indicatorElement;
    private bool isMainIndicator;
    public bool isPlayerIndicator;

    private const float SEGMENTS = 32;      // 원/부채꼴 세분화
    private const float INV_SEGMENTS = 1 / SEGMENTS;

    public IndicatorElement Element => indicatorElement;
    public Mesh Mesh => mesh;

    public void Init(IndicatorElement element, Material indicatorMaterial, Mesh mesh, bool isPlayerIndicator)
    {
        indicatorElement = element;
        this.mesh = mesh;
        isMainIndicator = indicatorElement.IsMainIndicator;
        this.isPlayerIndicator = isPlayerIndicator;

        meshRenderer.material = indicatorMaterial;
        gameObject.SetActive(true);
    }

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

    public void DrawIndicator(Vector3 start, Vector3 mouse)
    {
        if (isMainIndicator)
        {
            switch (indicatorElement.type)
            {
                case SkillIndicatorType.Line:
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
                    transform.position = GetElementEndPoint(start, mouse, indicatorElement);
                    transform.rotation = Quaternion.identity;
                    break;
            }
        }

        else
        {
            switch (indicatorElement.type)
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

    public static Vector3 GetElementEndPoint(Vector3 startPoint, Vector3 mouse, IndicatorElement element)
    {
        Vector3 direction = (mouse - startPoint).normalized;
        if (direction == Vector3.zero)
            direction = Vector3.forward;

        switch (element.type)
        {
            case SkillIndicatorType.Line:
                return startPoint + direction * element.length;

            case SkillIndicatorType.Sector:
                float halfAngleRad = element.angle * 0.5f * Mathf.Deg2Rad;
                return startPoint + direction * (element.radius * Mathf.Cos(halfAngleRad) * 2f);

            case SkillIndicatorType.Circle:
                if ((mouse - startPoint).magnitude > element.maxDistance)
                    return startPoint + direction.normalized * element.maxDistance;
                else
                    return mouse;

            default:
                return startPoint;
        }
    }

}