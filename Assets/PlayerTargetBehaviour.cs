using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class PlayerTargetBehaviour : MonoBehaviour, Vuforia.ITrackableEventHandler
{
    public GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        gameManager.OnTrackableStateChanged(gameObject, previousStatus, newStatus);
    }
}
