using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MouseLook : MonoBehaviour {
    [SerializeField] float mouseSensitivity = 100f;
    [SerializeField] [Range(0,1)] float borderSize = 10;
    [SerializeField] Camera cam;

    float minX, maxX, minY, maxY;
    Vector2 midPoint;

    void Start() {
        minX = borderSize;
        maxX = 1 - borderSize;
        minY = borderSize;
        maxY = 1 - borderSize;
        midPoint = new Vector2(0.5f, 0.5f);
    }

    // Update is called once per frame
    void Update() {
        Vector2 mousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        if (mousePos.x < minX || mousePos.x > maxX || mousePos.y < minY || mousePos.y > maxY) {
            Vector2 movement = mousePos - midPoint;
            transform.Translate(movement.normalized * mouseSensitivity * Time.deltaTime);
        }
    }
}
