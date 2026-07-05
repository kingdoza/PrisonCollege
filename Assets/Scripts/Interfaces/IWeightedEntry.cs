using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeightedEntry<T>
{
    T Value { get; }
    float Chance { get; }
}
