using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour {

	public GameObject itemPrefab; // 아이템 프리팹
    public GameObject powerItemPrefab;  // 황금 캣닢
    public GameObject magneticPrefab; // 자석
    public GameObject shieldItemPrefab;
    public float spawnInterval = 1f; // 스폰 간격
	public float spawnX = 10f; // x위치에 스폰
	public float spawnY = -1f; // y위치에 스폰


	void Start () {
        InvokeRepeating("SpawnItem", 3f, spawnInterval);

    }

    // Update is called once per frame
    void SpawnItem() {

        GameObject prefabToSpawn;

        // 10% 확률로 파워 아이템 생성
        if (Random.value < 0.03f)
        {
            prefabToSpawn = powerItemPrefab;

        }
        else if (Random.value < 0.05f)
        {
            prefabToSpawn = magneticPrefab;
        }
        else if (Random.value < 0.05f)
        {
            prefabToSpawn = shieldItemPrefab;
        }
        else
        {
            prefabToSpawn = itemPrefab;
        }

        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);


    }
}
