using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 激光瞄准线组件。从 firePoint 向 targetPosition 发射，撞墙时镜面反射。
/// 需要配合 LineRenderer 使用，建议挂载在 ShootAction 所在单位上。
/// </summary>
public class LaserBeam : MonoBehaviour
{
    [Header("发射点引用")]
    [SerializeField] private Transform firePoint;

    [Header("射线参数")]
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private int maxReflectCount = 3;

    [Header("视觉效果")]
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float lineWidth = 0.05f;

    private LineRenderer lineRenderer;
    private UnitStat unitStat;
    private bool isActive = false;

    private void Awake()
    {
        unitStat = GetComponentInParent<UnitStat>();
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        if (!isActive) return;
        if (firePoint == null) return;

        // 只有当前选中单位才能发射激光
        if (!IsSelectedUnit()) return;

        Vector3 origin = firePoint.position;
        Vector3 direction = (Events.LaserTargetPosition - origin).normalized;

        List<Vector3> points = new List<Vector3> { origin };
        float maxDist = (unitStat != null) ? unitStat.battleStats.OBPrecision * 100f : 50f;
        float remainingDistance = maxDist;

        for (int i = 0; i <= maxReflectCount; i++)
        {
            if (remainingDistance <= 0f) break;

            Ray ray = new Ray(origin, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, remainingDistance, wallLayerMask))
            {
                // 撞墙，记录碰撞点并反射
                points.Add(hit.point);

                direction = Vector3.Reflect(direction, hit.normal);
                float traveled = (hit.point - origin).magnitude;
                remainingDistance -= traveled;
                origin = hit.point + direction * 0.01f; // 偏移防止穿模
            }
            else
            {
                // 未撞墙，终点为最大射程处
                points.Add(origin + direction * remainingDistance);
                break;
            }
        }

        // 绘制激光
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    /// <summary>
    /// 进入瞄准状态时调用，开启激光
    /// </summary>
    public void Activate()
    {
        isActive = true;
        lineRenderer.enabled = true;
    }

    /// <summary>
    /// 退出瞄准状态时调用，关闭激光
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private void OnEnable()
    {
        Events.OnAimStateEntered += Activate;
        Events.OnAimStateExited += Deactivate;
    }

    private void OnDisable()
    {
        Events.OnAimStateEntered -= Activate;
        Events.OnAimStateExited -= Deactivate;
    }

    private void OnDestroy()
    {
        Deactivate();
    }

    /// <summary>
    /// 检查自身单位是否为当前选中单位
    /// </summary>
    private bool IsSelectedUnit()
    {
        if (UnitActionSystem.Instance == null || unitStat == null) return false;
        var selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return false;
        return selectedUnit.GetComponent<UnitStat>() == unitStat;
    }
}
