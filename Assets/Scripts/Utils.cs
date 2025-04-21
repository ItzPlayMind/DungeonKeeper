using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static GameObject FindByTagAndInstanceID(string tag, int instanceID)
    {
        var gbs = GameObject.FindGameObjectsWithTag(tag);
        foreach (var item in gbs)
        {
            if (item.GetInstanceID() == instanceID) return item;
        }
        return null;
    }
}
