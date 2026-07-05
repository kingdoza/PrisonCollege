using UnityEngine;

public class PosterDecorator : ItemDecorator
{
    protected override bool GetItemActivation()
    {
        return AttributeSystem.Instance.IsOtakuPoster;
    }
}
