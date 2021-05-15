using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleDrawer : MonoBehaviour
{
    [SerializeField] int numSegments = 1;
    [SerializeField] float testRadius = 1;
    [SerializeField] LineRenderer line = null;

    // Start is called before the first frame update
    void Start()
    {
        line.positionCount = numSegments;
    }

    [ContextMenu("set radius")]
    void Test() {
        line.positionCount = numSegments;
        SetRadius(testRadius);
    }
    public void SetRadius(float r) {
        float angleIncrement = 2 * Mathf.PI / numSegments;
        float angle = 0;
        for (int i = 0; i < numSegments; i++) {
            float x = Mathf.Sin(angle) * r;
            float y = Mathf.Cos(angle) * r;

            line.SetPosition(i, new Vector3(x, y, 0));

            angle += angleIncrement;
        }
    }
}
