using UnityEngine;

public class PowerItem : MonoBehaviour
{
    void Update()
    {
        transform.Translate(Vector3.left * 2f * Time.deltaTime);
        if (transform.position.x < -15f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>().ActivatePowerUp();
            Destroy(gameObject);
        }
    }
}