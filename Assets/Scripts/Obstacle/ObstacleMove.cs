using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float baseSpeed = 5f; // 기본 속도
   

    public float speedIncreaseInterval = 10f;
    public float speedIncrement = 10f;
    private float timer = 0f;

    public float destroyX = -15f;

    void Update()
    {
        
        transform.Translate(Vector3.left * baseSpeed * Time.deltaTime);

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

   
}