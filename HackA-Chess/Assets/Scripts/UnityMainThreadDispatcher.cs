using UnityEngine;
using System;
using System.Collections.Generic;

public class UnityMainThreadDispatcher : MonoBehaviour // hàm vận chuyển threads
{
    public static UnityMainThreadDispatcher Instance;

    private readonly Queue<Action> actions = new Queue<Action>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Enqueue(Action action) // Hàm thêm vào queue (hàng đợi)
    {
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }

    void Update() // cập nhật lên main threads
    {
        lock (actions)
        {
            while (actions.Count > 0)
            {
                var a = actions.Dequeue();
                a?.Invoke(); // chạy trên main thread
            }
        }
    }
}
