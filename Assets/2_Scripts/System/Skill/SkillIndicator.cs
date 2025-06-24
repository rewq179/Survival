using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

public enum SkillIndicatorType
{
    Line,
    Sector,
    Circle
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkillIndicator : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    private SkillIndicatorType indicatorType;
    private float length = 5f;      // 직선, 부채꼴 반지름, 원 반지름
    private float angle = 60f;      // 부채꼴 각도
    private float width = 1f;       // 직선 두께
    private const int SEGMENTS = 32;      // 원/부채꼴 세분화

    public void Init(SkillIndicatorType indicatorType, float length, float angle, float width, Material indicatorMaterial)
    {
        this.indicatorType = indicatorType;
        this.length = length;
        this.angle = angle;
        this.width = width;
        meshRenderer.material = indicatorMaterial;
    }

    public void Reset()
    {
        if (meshFilter.mesh != null)
        {
            DestroyImmediate(meshFilter.mesh);
            meshFilter.mesh = null;
        }
        
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    public void DrawIndicator(Vector3 position)
    {
        transform.position = position;

        meshFilter.mesh = indicatorType switch
        {
            SkillIndicatorType.Line => CreateLineMesh(length, width),
            SkillIndicatorType.Sector => CreateSectorMesh(angle, length, SEGMENTS),
            SkillIndicatorType.Circle => CreateCircleMesh(length, SEGMENTS),
            _ => null,
        };
    }

    // 직선 메시 생성
    private Mesh CreateLineMesh(float length, float width)
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

    // 부채꼴 메시 생성
    private Mesh CreateSectorMesh(float angle, float radius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3> { Vector3.zero };
        List<int> triangles = new List<int>();

        float angleStep = angle / segments;
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle * 0.5f + angleStep * i;
            float rad = Mathf.Deg2Rad * currentAngle;
            vertices.Add(new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius));
        }

        for (int i = 1; i <= segments; i++)
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

    // 원형 메시 생성
    private Mesh CreateCircleMesh(float radius, int segments)
    {
        return CreateSectorMesh(360f, radius, segments);
    }
}