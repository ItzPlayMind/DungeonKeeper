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

    public static bool IsEnemy(this GameObject gameObject, GameObject enemy, ref CharacterStats characterStats)
    {
        if (enemy == gameObject)
            return false;
        if (enemy.gameObject.layer == gameObject.layer) return false;
        characterStats = enemy.GetComponent<CharacterStats>();
        return (characterStats != null && !characterStats.IsDead);
    }
}
