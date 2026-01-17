using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    [SerializeField] private TMP_Text counterText; // UIñ≥ÇµÇ»ÇÁñ¢ê›íËÇ≈OK
    public int Count { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void AddCount(int add)
    {
        Count += add;

        if (counterText != null)
            counterText.text = $"IN: {Count}";

        Debug.Log($"Ball In! Count = {Count}");
    }
}
