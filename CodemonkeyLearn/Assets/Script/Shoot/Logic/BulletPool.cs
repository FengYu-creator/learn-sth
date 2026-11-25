using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 子弹对象池，管理子弹实例的创建和回收
/// 需要在场景中创建一个 GameObject 挂载此组件，并指定子弹预制体
/// </summary>
public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("子弹预制体")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("对象池设置")]
    [SerializeField] private int initialPoolSize = 10;

    private Queue<Bullet> availableBullets = new Queue<Bullet>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 预生成子弹
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateBullet();
        }
    }

    /// <summary>
    /// 获取一颗子弹
    /// </summary>
    public Bullet GetBullet()
    {
        // 尝试从池中取
        while (availableBullets.Count > 0)
        {
            Bullet bullet = availableBullets.Dequeue();
            // 检查子弹是否有效（可能已被 Destroy）
            if (bullet != null && bullet.gameObject != null)
            {
                bullet.gameObject.SetActive(true);
                return bullet;
            }
        }

        // 池空了，直接创建新的
        return CreateBullet();
    }

    /// <summary>
    /// 归还子弹到池中
    /// </summary>
    public void ReturnBullet(Bullet bullet)
    {
        if (bullet == null) return;
        bullet.gameObject.SetActive(false);
        availableBullets.Enqueue(bullet);
    }

    /// <summary>
    /// 创建新的子弹实例
    /// </summary>
    private Bullet CreateBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab is null!");
            return null;
        }

        var obj = Instantiate(bulletPrefab, transform);
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = obj.AddComponent<Bullet>();
        }
        return bullet;
    }
}
