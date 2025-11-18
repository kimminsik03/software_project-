using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMagnet : MonoBehaviour {

	public float magnetRange = 3f;
	// 자석 범위는 3

	public float magnetForce = 5f; // 자석 빨아드리는 힘
	private bool isMagnetActive = false; // 자석이 지금 활성화 되어있는지 
	private float magnetTimer = 0f; // 자석 시간



	// Update is called once per frame
	void Update () {
		if (isMagnetActive)
		{ // 자석이 활성화되어있는동안 
			magnetTimer -=	Time.deltaTime; // 시간이 흘러가서 점차 감소됨
            if (magnetTimer <= 0f)
                isMagnetActive = false; // 일정 시간이 지나면 비활성화

			AttractItems(); // 아이템 빨아드리기

        }
    }

	public void AttractItems()
	{
        Collider2D[] items = Physics2D.OverlapCircleAll(transform.position, magnetRange);
        // 플레이어 기준 가상의 중첩 원을 생성해서 그 범위에 있는 아이템들을 빨아 드림
		foreach(var item in items)
		{ // 범위 내에 있는 아이템들이 여러개 일수 있으니까 배열 순회
			if(item.CompareTag("Item"))
			{   // 그 원 범위 안에 있는 것이 아이템이라는 태그를 가지면
				// 플레이어 위치에서 아이템 위치를 빼고 보정해서 
				// 마치 플레이어에게 오는 것처럼 한다. 
                Vector3 dir = (transform.position - item.transform.position).normalized;
                item.transform.position += dir * magnetForce * Time.deltaTime;

            }
        }




    }

    public void ActivateMagnet(float duration)
    {
        isMagnetActive = true;
        magnetTimer = duration;
    }


}
