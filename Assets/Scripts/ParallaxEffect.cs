using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [SerializeField] Transform playerPosition;

    [SerializeField] Transform FarBackground;
    [SerializeField] Transform MiddleBackground;
    [SerializeField] Transform ClosestBackground;

    [SerializeField] float closeBackgroundMovementSpeed;
    [SerializeField] float middleBackgroundMovementSpeed;
    [SerializeField] float farBackgroundMovementSpeed;


    // Update is called once per frame
    void LateUpdate()
    {

        float CloseBackgroundPosX = playerPosition.position.x * closeBackgroundMovementSpeed;
        float MiddleBackgroundPosX = playerPosition.position.x * middleBackgroundMovementSpeed;
        float FarBackgroundPosX = playerPosition.position.x * farBackgroundMovementSpeed;


        ClosestBackground.position = new Vector3(CloseBackgroundPosX, ClosestBackground.position.y);
        MiddleBackground.position = new Vector3(MiddleBackgroundPosX, MiddleBackground.position.y);
        FarBackground.position = new Vector3(FarBackgroundPosX, FarBackground.position.y);
    }
}
