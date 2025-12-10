using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] Animator _anim;
    [SerializeField] GameObject firstPart;
    [SerializeField] GameObject secondPart;
    [SerializeField] CinemachineCamera cam;
    [SerializeField] GameObject newPlayer;
    [SerializeField] Transform secondRaft;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            _anim.SetTrigger("FadeOut");
            StartCoroutine(SceneChange());

        }
    }
    
    IEnumerator SceneChange()
    {
        yield return new WaitForSeconds(1f);
        firstPart.SetActive(false);
        secondPart.SetActive(true);
        cam.Follow = newPlayer.transform;

        _anim.SetTrigger("FadeIn");
        Cappa cap = FindAnyObjectByType<Cappa>();
        cap.transform.position = new Vector3(secondRaft.transform.position.x, cap.transform.position.y, cap.transform.position.z);
        cap.player = newPlayer.transform;
        FindAnyObjectByType<CappaAttacks>().player = newPlayer.transform;
        yield return new WaitForSeconds(10);
        // cap.spriteRenderer.enabled = true;
        cap.stalling = false;
        
    }
}
