using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppChecksumChecker : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(AwaitTime());
    }

    private IEnumerator AwaitTime()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(1);
    }
}
