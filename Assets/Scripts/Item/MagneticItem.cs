using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagneticItem : MonoBehaviour {

	public float magnetDuration = 5f;
	// 자석 효과 지속 효과 


	void OnTriggerEnter2D(Collider2D other)
	{ // 아이템이랑 상호작용하면 부딪히면
		if(other.CompareTag("Player"))
		{ // 플레이어랑 부딪힌거라면
			PlayerMagnet magnet = other.GetComponent<PlayerMagnet>();
			// 플레이어 마그넷의 컴포넌트를 가져온다.
			if(magnet != null )
			{
				magnet.ActivateMagnet(magnetDuration); // 지속시간동안 자석 활성호ㅓ
				Destroy(gameObject); // 없어져야하기 때문에. 
			}


        }

    }
}
