using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] Animator _anim;
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
        }
    }
    
    IEnumerator SceneChange()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Level2");
    }
}
