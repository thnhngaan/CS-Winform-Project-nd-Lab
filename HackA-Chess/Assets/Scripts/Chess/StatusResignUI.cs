using TMPro;
using UnityEngine;
using System.Collections;

public class StatusResignUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float autoHideSeconds = 1.2f;

    Coroutine _co;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(string msg)
    {
        if (text != null) text.text = msg;

        gameObject.SetActive(true);

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(AutoHide());
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSecondsRealtime(autoHideSeconds);
        gameObject.SetActive(false);
        _co = null;
    }
}
