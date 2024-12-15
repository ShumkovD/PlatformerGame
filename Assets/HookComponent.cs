using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookComponent : MonoBehaviour
{
    Transform player;

    public float DistanceToHook;
    public bool hookable;

    private void Start()
    {
        player = GameObject.Find("PlayerObject").GetComponent<Transform>();
    }

    private void Update()
    {
        if(Vector2.Distance(transform.position, player.position) <= DistanceToHook)
        {
            hookable = true;
        }
        else
        {
            hookable = false;
        }
    }
}
