using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RoomTransfer : MonoBehaviour
{
    void Update()
    {
        if (RoomNum == 1)
        {
            offset = new Vector3(-2.82f, 19.53f, 0);
        }
        else if (RoomNum == 2)
        {
            offset = new Vector3(-2.99f, 28.87f, 0);
        }
        else if (RoomNum == 3)
        {
            offset = new Vector3(-3.08f, 38.57f, 0);
        }
    }

    public Vector3 offset = new Vector3(-2.23f, 9.23f, 0); 
    public Transform player;
    public static bool CameraMove = false;
    public int RoomNum = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        print("Trigger Entered");

        if (other.CompareTag("Player"))
        {
            other.transform.position = offset;
            CameraMove = true;
            CameraFollow.Instance.MoveCamera();
            //RoomNum += 1;
            print(RoomNum);
        }
    }
}
