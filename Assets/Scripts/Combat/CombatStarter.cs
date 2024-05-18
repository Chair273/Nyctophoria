using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CombatStarter : MonoBehaviour
{
    private List<string> validEnemies = new List<string> { "Skeleton", "Crypt Keeper" };

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            int amount = Random.Range(1, 4);

            for (int i = 0; i < amount; i++)
            {
                Manager.AddCharacter(validEnemies[Random.Range(0, validEnemies.Count)]);
            }

            SceneManager.LoadScene("Combat");
        }
    }
}