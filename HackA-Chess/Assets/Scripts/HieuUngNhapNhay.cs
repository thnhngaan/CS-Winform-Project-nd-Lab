using UnityEngine;
using TMPro;

public class PulseText : MonoBehaviour
{
    public TMP_Text text;
    public float scaleAmount = 0.1f;
    public float speed = 2f;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = originalScale * scale;
    }
}
