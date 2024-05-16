using System.Collections.Generic;
using UnityEngine;

public class RoomTransfer : MonoBehaviour
{
    public Transform player;

    private List<Vector3> roomPositions = new List<Vector3> { new Vector3(-2.82f, 19.53f, 0), new Vector3(-2.99f, 28.87f, 0), new Vector3(-3.08f, 38.57f, 0) };

    private int roomNum = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            roomNum++;

            other.transform.position = roomPositions[roomNum];
            Camera.main.transform.position = roomPositions[roomNum];
        }
    }
}
