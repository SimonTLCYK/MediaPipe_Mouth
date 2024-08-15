using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitLineManager : MonoBehaviour
{
    public GameManager GameManager;
    // Start is called before the first frame update
    void Start()
    {
        GameManager = GameObject.FindWithTag("Game Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Ball ball = other.gameObject.GetComponent<Ball>();
        if (GameManager.check(ball))
        {
            GameManager.addScore();
        }

    }
}
