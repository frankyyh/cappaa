using JetBrains.Annotations;
using UnityEngine;
using System.Collections;

public class BGMover : MonoBehaviour
{
    [SerializeField] float speed;
    public bool isMoving;
    [SerializeField] float duration = 10;
    [SerializeField] GameObject _beginningSectionHolder;
    [SerializeField] Vector3 _beginningSectionStart;
    [SerializeField] Vector3 _beginningSectionEnd;
    [SerializeField] GameObject _endingSectionHolder;
    [SerializeField] Vector3 _endSectionStart;
    [SerializeField] Vector3 _endSectionEnd;
    public bool canMoveBeginning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(ElevatorMovement());
        _beginningSectionStart = _beginningSectionHolder.transform.position;
        _beginningSectionEnd = _beginningSectionStart - new Vector3 (30, _beginningSectionStart.y, _beginningSectionStart.z);
        _endSectionStart = _endingSectionHolder.transform.position;
        _endSectionEnd = _endSectionStart - new Vector3(30, _endSectionStart.y, _endSectionStart.z);

    }

    // Update is called once per frame
    void Update()
    {
        transform.position -= Vector3.right * Time.deltaTime * speed;


    }
    public float SmoothProgress(float progress)
    {
        progress = Mathf.Lerp(-Mathf.PI / 2, Mathf.PI / 2, progress);
        progress = Mathf.Sin(progress);
        progress = (progress / 2f) + .5f;

        return progress;
    }

    IEnumerator ElevatorMovement()
    {
        isMoving = true;
        // WaitForFixedUpdate wait = new();
        Debug.Log("started)");
        float progress;
        float time = 0f;
        Vector3 curPos;
        while (time < duration)
        {
            Debug.Log($"Time is {time} Duration is {duration}");
            progress = time / duration;
            float smoothProgress;

            smoothProgress = SmoothProgress(progress);

            curPos = Vector3.Lerp(_beginningSectionStart, _beginningSectionEnd, smoothProgress);
            // gameObject.GetComponent<Rigidbody2D>().MovePosition(curPos);
            time += Time.deltaTime;

            yield return null;
        }

        // if (!hasMoved)
        // {

        // }
        // else
        // {
        //     Debug.Log("Move the elevator back to original position");
        //     time = 0f;
        //     while (time < duration)
        //     {

        //         // Ping-pong progress between 0 and 1
        //         progress = Mathf.PingPong(time / duration, 1f);
        //         float smoothProgress = SmoothProgress(progress);

        //         curPos = Vector2.Lerp(endPoint, startPoint, smoothProgress);
        //         transform.position = curPos;

        //         time += Time.deltaTime;

        //         yield return new WaitForFixedUpdate();
        //     }
        //     hasMoved = false;
        //     Debug.Log("Has Moved is now " + false);

        // }



    }
}
