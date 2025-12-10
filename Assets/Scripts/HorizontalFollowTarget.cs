using UnityEngine;

public class HorizontalFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform player;

    private float fixedY;
    private float fixedZ;

    private void Awake()
    {
        var pos = transform.position;
        fixedY = pos.y;
        fixedZ = pos.z;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        var pos = transform.position;
        pos.x = player.position.x; // follow player horizontally

        // keep Y/Z fixed so jumping doesn't move the camera vertically
        pos.y = fixedY;
        pos.z = fixedZ;

        transform.position = pos;
    }
}
