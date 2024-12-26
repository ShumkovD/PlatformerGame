using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    public Transform target;

    public Transform LeftLowBorder;
    public Transform RightTopBorder;


    float vertExtent;
    float horzExtent;
    private void Start()
    {
        vertExtent = GetComponent<Camera>().orthographicSize;
        horzExtent = vertExtent * Screen.width / Screen.height;
    }
    // Update is called once per frame
    void Update()
    {
        Vector3 newPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        if(newPosition.y - vertExtent < LeftLowBorder.position.y)
        {
            newPosition.y = vertExtent + LeftLowBorder.position.y;
        }

        if (newPosition.x - horzExtent < LeftLowBorder.position.x)
        {
            newPosition.x = horzExtent + LeftLowBorder.position.x;
        }

        if (newPosition.y + vertExtent > RightTopBorder.position.y)
        {
            newPosition.y = RightTopBorder.position.y - vertExtent;
        }

        if (newPosition.x + horzExtent > RightTopBorder.position.x)
        {
            newPosition.x =  RightTopBorder.position.x - horzExtent;
        }

        transform.position = newPosition;
    }
}
