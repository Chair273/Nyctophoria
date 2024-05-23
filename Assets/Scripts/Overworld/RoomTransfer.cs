using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using TMPro;

public class RoomTransfer : MonoBehaviour
{
    private Vector2Int roomIndex;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && roomIndex != null)
        {
            MainManager.roomManager.ChangeRoom(roomIndex);
        }
    }

    public void SetRoomIndex(Vector2Int roomIndex)
    {
        this.roomIndex = roomIndex;
        transform.Find("Canvas").Find("Text").GetComponent<TextMeshProUGUI>().text = "(" + roomIndex.x + "," + roomIndex.y + ")";

        StartCoroutine(enable());
    }

    private IEnumerator enable()
    {
        yield return new WaitForSecondsRealtime(Random.Range(1.5f, 2.5f));
        transform.GetComponent<CircleCollider2D>().enabled = true;
        transform.Find("Light").GetComponent<Light2D>().enabled = true;
        //transform.Find("Particle System").GetComponent<ParticleSystem>().Play();
    }
}
