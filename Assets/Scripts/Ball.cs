using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private Rigidbody rb;
    private Renderer ballRenderer;
    public int moveSpeed;
    public Mouth.ShapeType ballShape;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ballRenderer = GetComponent<Renderer>();
        switch (ballShape)
        {
            case Mouth.ShapeType.a:
                ballRenderer.material.SetColor("_Color", Color.red);
                break;
            case Mouth.ShapeType.i:
                ballRenderer.material.SetColor("_Color", Color.blue);
                break;
            case Mouth.ShapeType.u:
                ballRenderer.material.SetColor("_Color", Color.green);
                break;
            default:
                ballRenderer.material.SetColor("_Color", Color.white);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        rb.velocity = new Vector3(-moveSpeed, 0, 0);

        if (transform.position.x < -110)
        {
            Destroy(gameObject);
        }
    }

    public void setBallShape(int ballShape)
    {
        switch (ballShape)
        {
            case 0:
                this.ballShape = Mouth.ShapeType.a;
                break;
            case 1:
                this.ballShape = Mouth.ShapeType.i;
                break;
            case 2:
                this.ballShape = Mouth.ShapeType.u;
                break;
            default :
                this.ballShape = Mouth.ShapeType.None;
                break;
        }
    }
}
