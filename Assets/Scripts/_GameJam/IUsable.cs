using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IUsable
{
    string AnimName { get; set; }
    public UnityEvent UseChangeEvent { get; set; }
    void Use();
}
