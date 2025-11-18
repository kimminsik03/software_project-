using UnityEngine;
using UnityEngine.UI;

public class ScorePopup : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeDuration = 1f;

    private Text text;
    private Color originalColor;
    private float timer = 0f;

    void Start()
    {
        text = GetComponent<Text>();
        originalColor = text.color;
    }

    void Update()
    {
        // 위로 이동
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 알파값 서서히 감소
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        // 제거
        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}