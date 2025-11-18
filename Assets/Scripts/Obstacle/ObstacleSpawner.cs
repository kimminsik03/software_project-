using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour {

	public GameObject obstaclePrefab; // 장애물 프리팹
									  // 프리팹으로 만들어놔서 그걸 할당해서 스크립트 사용

	public float spawnInterval = 10f; // 생성 간격 3초

	public float spawnX = 10f; // 생성 위치 x 
	public float spawnY = -2.5f; // 생성 위치 플레이어와 같은 높이

 

    void Start () {
        InvokeRepeating("SpawnObstacle", 3f, spawnInterval);
		//해당 함수를 1초 간격으로 반복한다

}

    // Update is called once per frame
    void SpawnObstacle() // 장애물 스폰
    {  // 해당 벡터 위치에 장애물 프리팹을 생성
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);


    }


  

}
