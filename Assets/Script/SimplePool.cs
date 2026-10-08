using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 极简通用对象池
/// </summary>
public class SimplePool
{
    private readonly GameObject prefab;
    private readonly Stack<GameObject> pool = new Stack<GameObject>();
    private readonly Transform parent;

    public SimplePool(GameObject prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Push(obj);
        }
    }

    public GameObject Get(Vector3 position)
    {
        GameObject obj = pool.Count > 0 ? pool.Pop() : Object.Instantiate(prefab, parent);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        obj.transform.SetParent(parent);
        pool.Push(obj);
    }
}