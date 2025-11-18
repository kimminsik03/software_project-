using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float baseSpeed = 5f; // 기본 속도
   

    public float speedIncreaseInterval = 10f;
    public float speedIncrement = 0.5f;
    private float timer = 0f;

    public float destroyX = -15f;

    void Update()
    {
        float currentSpeed = baseSpeed;
        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= speedIncreaseInterval)
        {
            baseSpeed += speedIncrement;
            timer = 0f;
        }

        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.isBoosted)
        {
            Destroy(gameObject); // 파워업 상태일 때만 파괴
        }
    }
}