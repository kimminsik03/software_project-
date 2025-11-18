using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpingObstacle : MonoBehaviour {

	public float jumpForce = 5f;
	public float jumpInterval = 2f;
	private float timer = 0f;

	private Rigidbody2D rb;

	void Start () {
		rb = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
		timer = Time.deltaTime;
		if(timer >= jumpInterval)
		{
			Jump();
			timer = 0f;
		}
	}

	void Jump()
	{
        if (Mathf.Abs(rb.velocity.y) < 0.01f) // 땅에 있을 때만 점프
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }


    }
}
