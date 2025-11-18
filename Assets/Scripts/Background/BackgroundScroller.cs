using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundScroller : MonoBehaviour {

	public float scrollSpeed = 2f;
	public float resetX = -20f; // 왼쪽 끝에 배치
	public float startX = 20f; // 오른쪽 재배치 

    // 점점 더 빨라지게 하기
    public float speedIncreaseInterval = 10f;
    public float speedIncrement = 0.5f;
    private Vector2 startPos;
    private float timer = 0f;

    // 속도 조정 이펙트 
    public GameObject speedTrailEffect;
    public GameObject speedUpText;
    public CameraShake cameraShake;

    

    void Start()
    {
        startPos = transform.position;
    }
    // Update is called once per frame
    void Update () {
        float currentSpeed = scrollSpeed;

        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);
        // 왼쪽으로 이동 배경들이 스크롤 되는 과정

        timer += Time.deltaTime;
        if (timer >= speedIncreaseInterval)
        {
            scrollSpeed += speedIncrement;
            timer = 0f;

            StartCoroutine(cameraShake.Shake(0.3f, 0.2f));

            // 바람선 이펙트
            speedTrailEffect.SetActive(true);
            Invoke("StopTrail", 1f);

            // 속도 증가 텍스트
            speedUpText.SetActive(true);
            Invoke("HideText", 1f);
        }

        if (transform.position.x < resetX)
        { // -20위치가 된다면? 위치 재배열
            Vector3 newPos = new Vector3(startX, transform.position.y, transform.position.z);
            transform.position = newPos;
        }

    }

    void StopTrail()
    {
        speedTrailEffect.SetActive(false);
    }

    void HideText()
    {
        speedUpText.SetActive(false);
    }

}
