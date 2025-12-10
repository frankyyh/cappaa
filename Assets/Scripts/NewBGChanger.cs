using UnityEngine;

public class NewBGChanger : MonoBehaviour
{
    [SerializeField] private GameObject[] backRowObjects;
    [SerializeField] private GameObject[] frontRowObjects;
    [SerializeField] private GameObject[] waterObjects;

    [SerializeField] GameObject[] middleGroundObjs;
    [SerializeField] GameObject[] fogObjs;
    [SerializeField]GameObject[] wayBack;

    [SerializeField] Vector3 spawnerLocation;
    [SerializeField] GameObject waterPrefab;

    // non-public, editable speeds (0-10)
    [SerializeField, Range(0f, 10f)] private float backRowSpeed = 3f;
    [SerializeField, Range(0f, 10f)] private float wayRowSpeed = 1f;

    [SerializeField, Range(0f, 10f)] private float frontRowSpeed = 5f;
    [SerializeField, Range(0f, 10f)] private float waterSpeed = 3f;
    [SerializeField, Range(0f, 10f)] private float middleGroundSpeed = 3f;
    [SerializeField, Range(0f, 10f)] private float fogSpeed = 3f;

    void Update()
    {
        MoveGroup(backRowObjects, backRowSpeed);
        MoveGroup(frontRowObjects, frontRowSpeed);
        MoveGroup(waterObjects, waterSpeed);
        MoveGroup(middleGroundObjs, middleGroundSpeed);
        MoveGroup(fogObjs, fogSpeed);
        MoveGroup(wayBack, wayRowSpeed);
    }

    private void MoveGroup(GameObject[] group, float speed)
    {
        if (group == null || speed <= 0f) return;
        Vector3 delta = Vector3.left * speed * Time.deltaTime;
        foreach (var obj in group)
        {
            if (obj != null)
                obj.transform.Translate(delta, Space.World);
        }
    }

    public void SpawnPrefab()
    {

    }
}
