using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class RoomTransfer : MonoBehaviour
{
    public Vector2Int roomIndex;

    private bool debounce = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && roomIndex != null && !debounce)
        {
            debounce = true;

            transform.Find("Light").GetComponent<Light2D>().enabled = false;
            transform.GetComponent<CircleCollider2D>().enabled = false;

            MainManager.roomManager.StartCoroutine(MainManager.roomManager.ChangeRoom(roomIndex));
        }
    }

    public void Activate()
    {
        debounce = true;

        transform.Find("Light").GetComponent<Light2D>().enabled = false;
        transform.GetComponent<CircleCollider2D>().enabled = false;

        MainManager.roomManager.StartCoroutine(DelayedEnable());
    }

    private IEnumerator DelayedEnable()
    {
        yield return new WaitForSecondsRealtime(Random.Range(0.5f, 2));
        yield return new WaitUntil(() => (RoomGenerator.main.player.position - transform.position).magnitude >= 0.5f);

        transform.Find("Light").GetComponent<Light2D>().enabled = true;
        transform.GetComponent<CircleCollider2D>().enabled = true;

        debounce = false;
    }
}
