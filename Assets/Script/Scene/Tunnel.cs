using UnityEngine;

public class Tunnel : MonoBehaviour
{
    public Transform entry;
    public Transform exit;
    public Transform getEntry()
    {
        if(entry != null) return entry;
        return null;
    }
    public Transform getExit()
    {
        if(exit != null) return exit;
        return null;
    }
}
