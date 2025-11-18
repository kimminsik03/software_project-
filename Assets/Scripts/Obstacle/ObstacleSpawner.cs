using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{

    public GameObject jumpObstaclePrefab;   // 플레이어가 '점프'해야 피하는 (지면) 장애물 프리팹
    public GameObject slideObstaclePrefab;  // 플레이어가 '슬라이드'해야 피하는 (천장) 장애물 프리팹

    [Header("Spawn Settings")]
    public float spawnInterval = 4.0f;      // 생성 간격 
    public float spawnX = 10f;             // 생성 위치 X

    // 1. 점프 장애물의 Y 위치 (지면 기준)
    public float baseSpawnY = -2.5f;

    // 2. 슬라이드 장애물의 Y 위치 (천장/빔처럼 높은 위치) - 이 값을 인스펙터에서 조정하여 원하는 높이를 설정하세요!
    public float slideObstacleCeilingY = 3f;


    void Start()
    {
        // 게임 시작 3초 후부터 spawnInterval 간격으로 장애물 생성 시작
        InvokeRepeating("SpawnObstacle", 3f, spawnInterval);
    }


    void SpawnObstacle() // 장애물 스폰
    {
        // 1. 생성할 장애물과 위치 Y 값을 결정합니다.
        GameObject obstacleToSpawn = null;
        float currentSpawnY;

        // 0 (슬라이드) 또는 1 (점프) 중 하나를 무작위로 선택
        int randomObstacleType = Random.Range(0, 2);

        if (randomObstacleType == 0) // 슬라이드 장애물
        {
            obstacleToSpawn = slideObstaclePrefab;
            // 🚨 슬라이드 장애물은 높은 위치(천장)에 생성됩니다.
            currentSpawnY = slideObstacleCeilingY;

        }
        else // 점프 장애물
        {
            obstacleToSpawn = jumpObstaclePrefab;
            // 점프 장애물은 기준 위치(지면)에 생성됩니다.
            currentSpawnY = baseSpawnY;
        }


        // 2. 프리팹이 할당되었는지 확인 후 생성합니다.
        if (obstacleToSpawn != null)
        {
            // Vector3(X, Y, Z=0)를 사용하여 2D 평면에 생성
            Vector3 spawnPos = new Vector3(spawnX, currentSpawnY, 0f);
            Instantiate(obstacleToSpawn, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogError("장애물 프리팹(Jump 또는 Slide)이 할당되지 않았습니다. 인스펙터를 확인하세요.");
        }
    }
}