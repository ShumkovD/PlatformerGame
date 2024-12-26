using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartbeatSystem : MonoBehaviour
{
    [SerializeField] int BPM;
    [SerializeField] int MaxBPM;
    [SerializeField] int MinBPM;

    [SerializeField] float BPMInterval;
    [SerializeField] float currentTime;
    [SerializeField] float BeatLength;


    [SerializeField] AudioSource Beat;

    enum BeatState
    {
        NoBeat,
        ToBeat,
        Beat,
        FromBeat,
    }

   [SerializeField] BeatState CurrentBeatState = BeatState.NoBeat;

    private void Update()
    {
        BPMInterval = (float)60 / BPM;

        currentTime += Time.deltaTime;

        switch (CurrentBeatState)
        {
            case BeatState.NoBeat:
                { 
                    if (currentTime > BPMInterval - BeatLength* 0.5f)
                    {
                        CurrentBeatState = BeatState.ToBeat;
                    }
                }
                break;
            case BeatState.ToBeat:
                {
                    CurrentBeatState = BeatState.Beat;
                }
                break;
            case BeatState.Beat:
                {
                    if (currentTime > BPMInterval)
                    {
                        Beat.Play();
                        CurrentBeatState = BeatState.FromBeat;
                    }
                }
                break;
            case BeatState.FromBeat:
                {
                    if (currentTime > BPMInterval + BeatLength * 0.5f)
                    {
                        CurrentBeatState = BeatState.NoBeat;
                        currentTime = 0;
                    }
                }
                break;
        }


    }

    public void ChangeBPM(int bpmChangeAmount)
    {
        int tempBPM = BPM + bpmChangeAmount;

        if (tempBPM > MaxBPM) tempBPM = MaxBPM;

        if (tempBPM < MinBPM)  tempBPM = MinBPM;


        BPM = tempBPM;
    }


}
