using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed;
    public float zoomSpeed;
    public float maxDistance;
    public float minDistance;
    public float maxYAngle;
    public float minYAngle;
    private float _rotationX;
    private float _rotationY;
    void Start()
    {
        
    }

    void Update()
    {

        transform.LookAt(target);
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            _rotationX += Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime;
            _rotationY += Input.GetAxisRaw("Vertical") * rotationSpeed * Time.deltaTime;
            _rotationY = Mathf.Clamp(_rotationY, minYAngle, maxYAngle);
            target.transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0);
        }
        if(Input.mouseScrollDelta.y > 0 && Vector3.Distance(target.position, transform.position) > minDistance)
        {
            transform.position += transform.forward * Time.deltaTime * zoomSpeed;
        }
        if (Input.mouseScrollDelta.y < 0 && Vector3.Distance(target.position, transform.position) < maxDistance)
        {
            transform.position += -transform.forward * Time.deltaTime * zoomSpeed;
        }

    }
}
