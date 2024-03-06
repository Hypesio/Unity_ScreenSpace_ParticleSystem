using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressToStart : MonoBehaviour
{
    public KeyCode keycode1 = KeyCode.None;
    public UnityEvent event1;
    public KeyCode keycode2 = KeyCode.None;
    public UnityEvent event2;
    public KeyCode keycode3 = KeyCode.None;
    public UnityEvent event3;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(keycode1))
        {
            event1.Invoke();
        }

        if (Input.GetKeyDown(keycode2))
        {
            event2.Invoke();
        }

        if (Input.GetKeyDown(keycode3))
        {
            event3.Invoke();
        }
    }
}
