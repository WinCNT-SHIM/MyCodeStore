using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Move : MonoBehaviour
{
    public bool upDown = false;
    public float speed = 1.0f;
    private const float Delta = 0.25f;

    Vector3 _pos;
    
    // Start is called before the first frame update
    void Start()
    {
        _pos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 v = _pos;
        if (upDown)
            v.y += Delta * Mathf.Sin(Time.time * speed);
        else
            v.x += Delta * Mathf.Sin(Time.time * speed);
        transform.position = v;
        
        transform.Rotate(0.5f, 0.0f, 0.1f, Space.Self);
    }
}
