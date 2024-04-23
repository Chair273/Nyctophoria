using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    private Tilemap tilemap;

    private Rigidbody2D Rigidbody2D;

    public float Speed;

    // Start is called before the first frame update
    void Start()
    {
        Rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        Vector3 MoveVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);

        Rigidbody2D.MovePosition(transform.position + MoveVector * Time.deltaTime * Speed);
    }
}
