using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private GameManager gameManager;
    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = FindAnyObjectByType<AudioManager>();
        gameManager = FindAnyObjectByType<GameManager>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Coin"))
        {
            Destroy(collision.gameObject); // đây là phương thức của Unity để xóa 
            gameManager.AddScore(1);
            audioManager.PlayCoinSound();
        }
        else if(collision.CompareTag("Trap"))
        {
            gameManager.GameOver();
        }
        else if(collision.CompareTag("Enemy"))
        {
            gameManager.GameOver();
        }
        else if(collision.CompareTag("Key"))
        {
            Destroy(collision.gameObject);  
            gameManager.GameWin();
        }
    }
}
