using UnityEngine;

public class DarkModeController : MonoBehaviour
{
    // Singleton instance
    private static DarkModeController _instance;
    public static DarkModeController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DarkModeController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DarkModeController");
                    _instance = go.AddComponent<DarkModeController>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [SerializeField] private bool _darkMode = false;
    
    public static bool DarkMode
    {
        get { return Instance._darkMode; }
        set 
        { 
            if (Instance._darkMode != value)
            {
                Instance._darkMode = value;
                Instance.OnDarkModeChanged?.Invoke(value);
            }
        }
    }
    
    // Event for dark mode state changes
    public System.Action<bool> OnDarkModeChanged;
    
    // Public property to access the private boolean state
    public bool IsDarkMode
    {
        get { return _darkMode; }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
