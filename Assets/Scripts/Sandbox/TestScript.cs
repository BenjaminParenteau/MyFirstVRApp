using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] private string message = "TestScript is running.";

    private void Start()
    {
        Debug.Log(message, this);
    }
}
