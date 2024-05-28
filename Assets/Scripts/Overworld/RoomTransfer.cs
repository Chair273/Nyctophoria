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
        WaitForSecondsRealtime waitTime = new WaitForSecondsRealtime(0.1f);

        yield return new WaitForSecondsRealtime(Random.Range(1.5f, 2.5f));

        if (RoomGenerator.main != null)
        {
            while ((RoomGenerator.main.player.position - transform.position).magnitude < 0.5f)
            {
                yield return waitTime;
            }
        }

        transform.Find("Light").GetComponent<Light2D>().enabled = true;
        transform.GetComponent<CircleCollider2D>().enabled = true;

        debounce = false;
    }
}
