using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRotation : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed;
    private float _rotationX;
    private float _rotationY;
    void Start()
    {
        _rotationY = 45;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target);
        if (Input.GetKey(KeyCode.LeftControl))
        {
            _rotationX += Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime;
            _rotationY += Input.GetAxisRaw("Vertical") * rotationSpeed * Time.deltaTime;
        }     
        target.transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
    }
}
