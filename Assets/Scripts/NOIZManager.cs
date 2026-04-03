using UnityEngine;

public class NOIZManager : MonoBehaviour
{
    [SerializeField] private NOIZSettings settings;
    [SerializeField] private NOIZHaptic haptics;

    public static NOIZSettings Settings;
    public static NOIZHaptic Haptics;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Settings = settings;
        Haptics = haptics;
    }   
}

public enum ScoreType
{
    Perfect,
    Great,
    Bad,
    Missed
}
