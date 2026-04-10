using System.Collections.Generic;
using UnityEngine;

public class RewardObjectPool : MonoBehaviour
{
    private Queue<RewardObject> _pool = new Queue<RewardObject>();
    private GameObject _prefab;
    private Transform _parent;

    public void Init(GameObject prefab, Transform parent, int initialSize)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNewObject();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public RewardObject Get()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            var obj = CreateNewObject();
            obj.gameObject.SetActive(true);
            return obj;
        }
    }

    public void Release(RewardObject obj)
    {
        obj.gameObject.SetActive(false);
        _pool.Enqueue(obj);
    }

    private RewardObject CreateNewObject()
    {
        var go = Instantiate(_prefab, _parent);
        return go.GetComponent<RewardObject>();
    }
}
