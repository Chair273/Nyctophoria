using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class RoomTransfer : MonoBehaviour
{
    public Vector2Int roomIndex;

    private bool debounce = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && roomIndex != null && !debounce && !MainManager.roomManager.transitionDebounce) 
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

        StartCoroutine(DelayedEnable());
    }

    private IEnumerator DelayedEnable()
    {
        WaitForEndOfFrame waitTime = new WaitForEndOfFrame();

        yield return new WaitForSecondsRealtime(Random.Range(1.5f, 3.0f));

        while (RoomGenerator.main != null && ((Vector2)RoomGenerator.main.player.position - (Vector2)transform.position).magnitude < 0.4f)
        {
            yield return waitTime;
        }

        transform.Find("Light").GetComponent<Light2D>().enabled = true;
        transform.GetComponent<CircleCollider2D>().enabled = true;

        debounce = false;
    }
}
