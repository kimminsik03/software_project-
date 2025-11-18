using UnityEngine;

public class Item : MonoBehaviour
{
    public float baseSpeed = 2f;



    void Update()
    {
       
        transform.Translate(Vector3.left * baseSpeed * Time.deltaTime);

        if (transform.position.x < -15f)
        {
            Destroy(gameObject);
        }

    }


    
}