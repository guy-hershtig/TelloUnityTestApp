using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeachBallBehaviour : MonoBehaviour
{
    private int _score = 0;

    public Text text;

    void Start()
    {
        this.GetComponent<Rigidbody>().AddForce(10 * Vector3.left);
    }

    void OnCollisionEnter(Collision c)
    {
        switch (c.collider.gameObject.name)
        {
            case "PlayerBat1":
            case "PlayerBat2":
                text.text = "Score: " + ++_score;
                break;
            case "LeftWall":
                text.text = "Collided with LeftWall";
                this.GetComponent<Rigidbody>().AddForce(20 * Vector3.right);
                break;
            case "RightWall":
                text.text = "Collided with RightWall";
                this.GetComponent<Rigidbody>().AddForce(20 * Vector3.left);
                break;
            default:
                text.text = "Collided with " + c.collider.gameObject.name;
                break;
        }
    }

    void OnCollisionExit(Collision c)
    {
    }
}
