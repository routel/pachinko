using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleCounter : MonoBehaviour
{
    [SerializeField] private int addCount = 1;
    [SerializeField] private string ballTag = "Ball";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        GameManager.Instance?.AddCount(addCount);
        Destroy(other.gameObject);
    }
}
