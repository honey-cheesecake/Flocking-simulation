using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTowardsTest : MonoBehaviour {
    [SerializeField] Vector3 from ;
    [SerializeField] float maxDegrees = 90f;
    [SerializeField] [Range(0,1)] float t = 90f;
    [SerializeField] float rotSpeed = 90f;

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.white;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(Vector3.zero, from);
        Gizmos.DrawLine(Vector3.zero, transform.position);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, Vector3.RotateTowards(from.normalized, transform.position.normalized, maxDegrees * Mathf.Deg2Rad, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Vector3.zero, Vector3.Lerp(from.normalized, transform.position.normalized, t).normalized);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, Rotate(from, Vector3.Cross(from,transform.position).z * rotSpeed));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Rotate(from, Mathf.Clamp(Vector2.SignedAngle(from, transform.position), -maxDegrees, maxDegrees)));
    }

    //rotates vector
    public Vector2 Rotate(Vector2 v, float degrees) {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
}
