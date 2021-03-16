using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public class Forceable : MonoBehaviour
{
    bool focused;

    public bool Focused
    {
        get
        {
            return focused;
        }

        set
        {
            if (!focused && value)
            {
                EnableFocus();
            }
            else if(focused && !value)
            {
                DisabelFocus();
            }

            focused = value;
        }
    }
    

    private void Start()
    {
        Outline outline = GetComponent<Outline>();
        outline.enabled = false;
        focused = false;
    }

    void EnableFocus()
    {
        Outline outline = GetComponent<Outline>();
        outline.enabled = true;
    }

    void DisabelFocus()
    {
        Debug.Log("End Focus");
        Outline outline = GetComponent<Outline>();
        outline.enabled = false;
    }
}
