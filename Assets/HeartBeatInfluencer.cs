using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartBeatInfluencer : MonoBehaviour
{
    HeartbeatSystem HBSystem;
    Transform PlayerObject;

    [SerializeField] float EffectRadius;
    [SerializeField] float ChangePerSecond; //3 -> 1 = 5

    private void Awake()
    {
         HBSystem = GameObject.Find("GlobalHeartBeatSystem").GetComponent<HeartbeatSystem>();
         PlayerObject = GameObject.Find("PlayerObject").transform;
    }

    float curTimer;


    private void Update()
    {
        if(Vector3.Distance( PlayerObject.position, transform.position) >= EffectRadius)
        {
            return;
        }

        curTimer += Time.deltaTime;
        if (curTimer > (1/ ChangePerSecond))
        {
            HBSystem.ChangeBPM(+1);
            curTimer = 0;
        }
    }

}
