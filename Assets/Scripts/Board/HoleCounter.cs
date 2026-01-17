using UnityEngine;

public class HoleCounter : MonoBehaviour
{
    public static HoleCounter Instance { get; private set; }

    public int startCount;
    public int prizeCount;
    public int outCount;

    private void Awake()
    {
        Instance = this;
    }

    public void OnHoleEntered(HoleType type)
    {
        switch (type)
        {
            case HoleType.Start:
                startCount++;
                break;
            case HoleType.Prize:
                prizeCount++;
                break;
            case HoleType.Out:
                outCount++;
                break;
        }

        Debug.Log($"HoleCounter: Start={startCount}, Prize={prizeCount}, Out={outCount}");
        // ここでUI更新（TextMeshProなど）
    }
}
