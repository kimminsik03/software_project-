using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldItem : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌하면 쉴드 활성화
        if (other.CompareTag("Player"))
        {
            // 플레이어 컨트롤러에서 쉴드 활성화 메서드를 호출합니다.
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ActivateShield();
            }

            // 아이템 획득 후 자신을 파괴
            Destroy(gameObject);
        }
    }
}
