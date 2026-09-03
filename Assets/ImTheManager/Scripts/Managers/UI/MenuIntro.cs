using UnityEngine;
using System.Collections;

public class MenuIntro : MonoBehaviour
{
    [SerializeField] RectTransform[] buttons;
    [SerializeField] float startOffsetX = 1200f;
    [SerializeField] float slideSpeed = 0.5f;
    [SerializeField] float delayBetween = 0.1f;

    void Start()
    {
        // mueve todos los botones fuera de pantalla
        foreach (var btn in buttons)
        {
            Vector2 pos = btn.anchoredPosition;
            btn.anchoredPosition = new Vector2(pos.x + startOffsetX, pos.y);
        }

        StartCoroutine(SlideIn());
    }

    IEnumerator SlideIn()
    {
        // espera un poco antes de animar
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < buttons.Length; i++)
        {
            StartCoroutine(SlideButton(buttons[i]));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator SlideButton(RectTransform btn)
    {
        Vector2 targetPos = new Vector2(btn.anchoredPosition.x - startOffsetX, btn.anchoredPosition.y);
        float elapsed = 0f;
        Vector2 startPos = btn.anchoredPosition;

        while (elapsed < slideSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideSpeed;
            // ease out para que desacelere al llegar
            t = 1f - Mathf.Pow(1f - t, 3f);
            btn.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        btn.anchoredPosition = targetPos;
    }
}