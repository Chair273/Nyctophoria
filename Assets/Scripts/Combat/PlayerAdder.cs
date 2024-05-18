using UnityEngine;

public class PlayerAdder : MonoBehaviour
{
    public string characterName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Manager.AddCharacter(characterName);
            Destroy(gameObject);
        }
    }
}