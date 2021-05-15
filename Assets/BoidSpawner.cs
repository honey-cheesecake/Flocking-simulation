using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;
using TMPro;

public class BoidSpawner : MonoBehaviour {
    [Header("Spawner")]
    [SerializeField] private int spawnCount = 30;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float mapRadius = 10f;
    [SerializeField] [Range(1, 10)] private int boidUpdateGroupCount = 2;

    [Header("Mouse Spawn")]
    [SerializeField] private int mouseSpawnCount = 10;
    [SerializeField] private float mouseSpawnRadius = 5f;
    [SerializeField] private AudioSource mousePlaceAudio = null;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsUI = null;
    [SerializeField] private float fpsRefreshPeriod = 1;
    private int frameCount = 0;
    private float nextFPSUpdateTime = 0;
    [SerializeField] private TextMeshProUGUI boidCountUI = null;
    private int boidCount = 0;
    [SerializeField] private Transform circleParent = null;

    [Header("Boid")]
    [SerializeField] private GameObject boidPrefab = null;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotSpeed = 1f;
    [Header("Repulsion")]
    [SerializeField] private float repulsionRange = 5f;
    private float sqrRepulsionRange;
    [SerializeField] private float repulsionWeight = 5f;
    [Header("Alignment")]
    [SerializeField] private float alignmentRange = 5f;
    private float sqrAlignmentRange;
    [SerializeField] private float alignmentWeight = 5f;
    [Header("Cohesion")]
    [SerializeField] private float cohesionRange = 5f;
    private float sqrCohesionRange;
    [SerializeField] private float cohesionWieght = 5f;

    // private NativeArray<Vector3> velocities;
    public TransformAccessArray boidTransforms;
    public NativeList<Vector3> prevFrameHeadings;
    public NativeList<Vector3> prevFramePoss;

    private MoveJob moveJob;
    private JobHandle moveJobHandle;
    private VariablesUpdateJob variablesUpdateJob;
    private JobHandle variablesUpdateJobHandle;

    void Start() {
        prevFrameHeadings = new NativeList<Vector3>(spawnCount, Allocator.Persistent);
        prevFramePoss = new NativeList<Vector3>(spawnCount, Allocator.Persistent);
        boidTransforms = new TransformAccessArray(spawnCount);
        SpawnBoids(spawnCount, Vector2.zero, spawnRadius);

        circleParent.parent = boidTransforms[0];
        circleParent.localPosition = Vector3.zero;

        UpdateRepulsionRange(repulsionRange);
        UpdateAlignmentRange(alignmentRange);
        UpdateCohesionRange(cohesionRange);
    }

    public void UpdateRepulsionRange(float repulsionRange) {
        sqrRepulsionRange = repulsionRange * repulsionRange;
    }
    public void UpdateRepulsionWeight(float _new) {
        repulsionWeight = _new;
    }
    public void UpdateAlignmentRange(float alignmentRange) {
        sqrAlignmentRange = alignmentRange * alignmentRange;
    }
    public void UpdateAlignmentWeight(float _new) {
        alignmentWeight = _new;
    }
    public void UpdateCohesionRange(float cohesionRange) {
        sqrCohesionRange = cohesionRange * cohesionRange;
    }
    public void UpdateCohesionWeight(float _new) {
        cohesionWieght = _new;
    }

    void SpawnBoids(int numberToAdd, Vector2 center, float radius) {
        for (int i = 0; i < numberToAdd; i++) {
            Vector2 spawnPos = center + Random.insideUnitCircle * radius;
            Transform t = Instantiate(boidPrefab, spawnPos, Quaternion.identity).transform;
            boidTransforms.Add(t);
            prevFramePoss.Add(spawnPos);
            prevFrameHeadings.Add((Vector2)Random.onUnitSphere);
        }
        boidCount += numberToAdd;
        boidCountUI.text = $"Boid Count: {boidCount}";
    }

    [BurstCompile]
    struct MoveJob : IJobParallelForTransform {
        public int groupToMove;
        public int groupCount;
        public float deltaTime;
        public float mapRadius;
        public float speed;
        public float rotSpeed;
        [ReadOnly] public NativeArray<Vector3> prevFramePoss;
        [ReadOnly] public NativeArray<Vector3> prevFrameHeadings;

        public float sqrRepulsionRange;
        public float repulsionWeight;
        public float sqrAlignmentRange;
        public float alignmentWeight;
        public float sqrCohesionRange;
        public float cohesionWeight;

        // Anything within that method will run once for every transform in transformAccessArray.
        public void Execute(int i, TransformAccess transform) {
            if (i % groupCount != groupToMove) {
                return;
            }

            Vector3 preferredHeading = Vector3.zero;

            // ##### repulsion #####
            float sqrDistanceToClosestNeighbor = sqrRepulsionRange;
            Vector3 preferedHeading_Repulsion = Vector3.zero;
            Vector3 preferredHeading_Alignment = Vector3.zero;
            Vector3 averageNeighborPos = Vector3.zero;
            int cohesionNeighborTally = 0;
            Vector3 preferredHeading_Cohesion = averageNeighborPos - transform.position;

            for (int j = 0; j < prevFramePoss.Length; j++) {
                if (j == i) continue;

                float sqrDistance = Vector3.SqrMagnitude(prevFramePoss[j] - transform.position);
                if (sqrDistance < sqrDistanceToClosestNeighbor && sqrDistance <= sqrRepulsionRange) {
                    // point away from nearby boids
                    sqrDistanceToClosestNeighbor = sqrDistance;
                    preferedHeading_Repulsion = transform.position - prevFramePoss[j];
                }
                if (sqrDistance <= sqrAlignmentRange) {
                    // average velocities of nearby boids
                    preferredHeading_Alignment += prevFrameHeadings[j];
                }
                if (sqrDistance <= sqrCohesionRange) {
                    // average velocities of nearby boids
                    ++cohesionNeighborTally;
                    averageNeighborPos += prevFramePoss[j];
                }
            }
            preferedHeading_Repulsion.Normalize();
            preferredHeading_Alignment.Normalize();

            averageNeighborPos = (cohesionNeighborTally == 0) ? transform.position : averageNeighborPos /= cohesionNeighborTally;
            preferredHeading_Cohesion = averageNeighborPos - transform.position;
            preferredHeading_Cohesion.Normalize();

            // calculate new heading
            preferredHeading = preferedHeading_Repulsion * repulsionWeight + preferredHeading_Alignment * alignmentWeight + preferredHeading_Cohesion * cohesionWeight;

            // Move
            float angleToRotate = Mathf.Clamp(Vector2.SignedAngle(prevFrameHeadings[i], preferredHeading), -rotSpeed * deltaTime, rotSpeed * deltaTime);
            Vector3 finalHeading = RotateVector2(prevFrameHeadings[i], angleToRotate);
            Vector3 newPos = transform.position + finalHeading * speed * deltaTime;
            newPos.z = 0;
            transform.position = newPos;
        }
    }

    private void Update() {
        variablesUpdateJobHandle.Complete();

        ++frameCount;
        if (nextFPSUpdateTime <= Time.unscaledTime) {
            fpsUI.text = $"FPS: {(int)(frameCount / fpsRefreshPeriod)}";
            frameCount = 0;
            nextFPSUpdateTime = Time.unscaledTime + fpsRefreshPeriod;
        }


        if (Input.GetMouseButtonDown(0)) {
            Vector2 mouseWorldPos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (mouseWorldPos.magnitude <= mapRadius) {
                SpawnBoids(mouseSpawnCount, mouseWorldPos, mouseSpawnRadius);
                mousePlaceAudio.Play();
            }
        }

        moveJob = new MoveJob() {
            groupCount = boidUpdateGroupCount,
            groupToMove = Time.frameCount % boidUpdateGroupCount,
            deltaTime = Time.deltaTime * boidUpdateGroupCount,
            mapRadius = mapRadius,
            speed = speed,
            rotSpeed = rotSpeed,
            prevFramePoss = prevFramePoss,
            prevFrameHeadings = prevFrameHeadings,
            sqrRepulsionRange = sqrRepulsionRange,
            repulsionWeight = repulsionWeight,
            sqrAlignmentRange = sqrAlignmentRange,
            alignmentWeight = alignmentWeight,
            sqrCohesionRange = sqrCohesionRange,
            cohesionWeight = cohesionWieght
        };
        moveJobHandle = moveJob.Schedule(boidTransforms);

    }

    [BurstCompile]
    struct VariablesUpdateJob : IJobParallelForTransform {
        public int groupToMove;
        public int groupCount;
        public NativeArray<Vector3> prevFramePoss;
        public NativeArray<Vector3> prevFrameHeadings;
        public float mapRadius;

        // public float mapRadius;

        public void Execute(int i, TransformAccess transform) {
            if (i % groupCount != groupToMove) {
                return;
            }

            prevFrameHeadings[i] = (transform.position - prevFramePoss[i]).normalized;

            // teleport to other side
            if (transform.position.magnitude >= mapRadius) {
                transform.position = -transform.position;
                transform.position *= 0.99f;
            }

            prevFramePoss[i] = transform.position;
        }
    }
    private void LateUpdate() {
        moveJobHandle.Complete();
        variablesUpdateJob = new VariablesUpdateJob() {
            groupCount = boidUpdateGroupCount,
            groupToMove = Time.frameCount % boidUpdateGroupCount,
            prevFramePoss = prevFramePoss,
            prevFrameHeadings = prevFrameHeadings,
            mapRadius = mapRadius
        };
        variablesUpdateJobHandle = variablesUpdateJob.Schedule(boidTransforms);
    }

    private void OnDestroy() {
        moveJobHandle.Complete();
        variablesUpdateJobHandle.Complete();

        boidTransforms.Dispose();
        prevFrameHeadings.Dispose();
        prevFramePoss.Dispose();
    }

    private void OnDrawGizmosSelected() {
        moveJobHandle.Complete();
        variablesUpdateJobHandle.Complete();
        Vector3 center = (prevFramePoss.IsCreated && prevFramePoss.Length > 0) ? prevFramePoss[0] : Vector3.zero;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, repulsionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, alignmentRange);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, cohesionRange);

        Gizmos.DrawWireSphere(Vector3.zero, mapRadius);
    }

    //rotates vector
    static Vector2 RotateVector2(Vector2 v, float degrees) {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
}
