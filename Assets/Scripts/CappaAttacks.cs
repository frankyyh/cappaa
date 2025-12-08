using System.Collections;
using UnityEngine;

public class CappaAttacks : MonoBehaviour
{


    [SerializeField] GameObject handsIndicator;
    [SerializeField] GameObject lurkIndicator;
    [SerializeField] GameObject handsAttack;
    [SerializeField] GameObject lurkAttack;

    [SerializeField] float _handsAttackIndicatorDelay;
    [SerializeField] float _lurkingAttackIndicatorDelay;

    [SerializeField] float _timeBetweenAttacks;
    [SerializeField] float _attackStartTime;


    [SerializeField] WaitForSeconds handsWait;

    // new: configure random delay range in Inspector
    [SerializeField] float minAttackDelay = 8f;
    [SerializeField] float maxAttackDelay = 10f;

    // timer
    float _attackTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetAttackTimer();
    }

    // Update is called once per frame
    void Update()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            // pick an attack at random
            if (Random.value < 0.5f)
                HandsAttack();
            else
                LurkingAttack();

            ResetAttackTimer();
        }
    }
    
    void ResetAttackTimer()
    {
        _attackTimer = Random.Range(minAttackDelay, maxAttackDelay);
    }
    
    void HandsAttack()
    {
        StartCoroutine(HandAttack());
    }
    
    IEnumerator HandAttack()
    {
        handsIndicator.SetActive(true);
        yield return new WaitForSeconds(_handsAttackIndicatorDelay);
        handsIndicator.SetActive(false);
        handsAttack.SetActive(true);
    }
    void LurkingAttack()
    {
        StartCoroutine(LurkAttack());

    }
    IEnumerator LurkAttack()
    {
        lurkIndicator.SetActive(true);
        yield return new WaitForSeconds(_lurkingAttackIndicatorDelay);
        lurkIndicator.SetActive(false);
        lurkAttack.SetActive(true);

    }
}
