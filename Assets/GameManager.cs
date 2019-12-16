using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class GameManager : MonoBehaviour
{
    public GameObject ball;
    public Text scoreText;
    public Text droneLogText;
    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject player1;
    public GameObject player2;
    public Button droneCommToggle;
    public Button droneMotorsToggle;
    public Button droneLeftButton;
    public Button droneRightButton;
    public Button droneForwardButton;
    public Button droneBackwardButton;
    public Sprite droneTurnCommOnSprite;
    public Sprite droneTurnCommOffSprite;
    public Sprite droneTurnMotorsOnSprite;
    public Sprite droneTurnMotorsOffSprite;
    public Sprite buttonDisabledSprite;
    public string TurnMotorsOffText = "Land";
    public string TurnMotorsOnText = "Take Off";
    public string CommOnText = "Comm Off";
    public string CommOffText = "Comm On";
    public PlaneFinderBehaviour planeFinder;

    public float ballDistance = 0.2F;
    public float ballThrowingFactor = 3;

    private bool _droneMotorsOn;
    private int _frameCount;

    private Vector2 _start;
    private TrackableBehaviour.Status _player1TrackableStatus;
    private TrackableBehaviour.Status _player2TrackableStatus;
    private TelloClientNative _telloClient;
    private Button[] _droneControlButtons;

    // Start is called before the first frame update
    void Start()
    {
        CameraDevice.Instance.SetField("exposure-time", "auto");
        CameraDevice.Instance.SetField("iso", "auto");
        scoreText.text = "Score: 0";

        droneMotorsToggle.GetComponentInChildren<Text>().text = string.Empty;
        droneMotorsToggle.image.sprite = buttonDisabledSprite;
        droneMotorsToggle.enabled = false;
        droneMotorsToggle.onClick.AddListener(ToggleDroneMotors);

        droneCommToggle.image.sprite = droneTurnCommOnSprite;
        droneCommToggle.GetComponentInChildren<Text>().text = CommOffText;
        droneCommToggle.enabled = true;
        droneCommToggle.onClick.AddListener(ToggleDroneComm);

        _droneControlButtons = new[]
        {
            droneLeftButton,
            droneRightButton,
            droneForwardButton,
            droneBackwardButton
        };
        foreach (var button in _droneControlButtons)
            button.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        var telloClient = _telloClient;
        try
        {
            if (telloClient != null)
            {
                if (++_frameCount == 60)
                {
                    _frameCount = 0;
                    droneLogText.text = string.Format("Drone: online ({0:F0}% battery)", _telloClient.BatteryPercent);
                }
                var leftButtonPressed = droneLeftButton.enabled && droneLeftButton.GetComponent<HoldableButton>().buttonPressed;
                var rightButtonPressed = droneRightButton.enabled && droneRightButton.GetComponent<HoldableButton>().buttonPressed;
                var forwardButtonPressed = droneForwardButton.enabled && droneForwardButton.GetComponent<HoldableButton>().buttonPressed;
                var backwardButtonPressed = droneBackwardButton.enabled && droneBackwardButton.GetComponent<HoldableButton>().buttonPressed;
                telloClient.MoveLeft = leftButtonPressed && !rightButtonPressed;
                telloClient.MoveRight = !leftButtonPressed && rightButtonPressed;
                telloClient.MoveForward = forwardButtonPressed && !backwardButtonPressed;
                telloClient.MoveBackward = !forwardButtonPressed && backwardButtonPressed;
                droneLogText.text = string.Format("Drone: {0}", 
                    telloClient.MoveLeft ? "Moving Left" :
                    telloClient.MoveRight ? "Moving Right" : "Holding Position");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }
    
    void OnApplicationQuit()
    {
        var client = _telloClient;
        if (client != null)
        {
            client.Close();
            _telloClient = null;
        }
    }

    public void OnTrackableStateChanged(GameObject gameObject, TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        //if (gameObject.name == player1.name)
    }

    public async void ToggleDroneComm()
    {
        try
        {
            if (_telloClient == null)
            {
                _telloClient = new TelloClientNative();
                droneCommToggle.GetComponentInChildren<Text>().text = CommOnText;
                droneCommToggle.image.sprite = droneTurnCommOffSprite;
                await _telloClient.StartAsync();
                droneMotorsToggle.image.sprite = droneTurnMotorsOnSprite;
                droneMotorsToggle.enabled = true;
                planeFinder.enabled = false;
            }
            else
            {
                _telloClient.Close();
                _telloClient = null;
                droneCommToggle.GetComponentInChildren<Text>().text = CommOffText;
                droneCommToggle.image.sprite = droneTurnCommOnSprite;
                droneMotorsToggle.image.sprite = buttonDisabledSprite;
                droneMotorsToggle.enabled = false;
                foreach (var button in _droneControlButtons)
                    button.enabled = false;
                droneLogText.text = "Drone: offline";
                planeFinder.enabled = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
            droneLogText.text = "Drone: " + ex.Message;
        }
    }

    public async void ToggleDroneMotors()
    {
        try
        {
            if (_telloClient != null)
            {
                droneMotorsToggle.enabled = false;
                if (!_droneMotorsOn)
                    _droneMotorsOn = await _telloClient.TakeOffAsync();
                else
                    _droneMotorsOn = !await _telloClient.LandAsync();
                droneMotorsToggle.enabled = true;
                droneMotorsToggle.image.sprite = _droneMotorsOn
                 ? droneTurnMotorsOffSprite
                 : droneTurnMotorsOnSprite;
                foreach (var button in _droneControlButtons)
                    button.enabled = _droneMotorsOn;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
            droneLogText.text = "Drone: " + ex.Message;
        }
    }

}
