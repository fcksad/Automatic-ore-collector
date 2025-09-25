using System.Collections.Generic;
using UnityEngine;

public static class WeightedDropSystem<T>
{
    /// <summary>
    /// √енерирует гарантированный список предметов.
    /// </summary>
    /// <param name="weights">¬еса: в процентах или абсолютные количества</param>
    /// <param name="totalCount">—колько всего предметов нужно</param>
    /// <param name="usePercent">true = веса Ч проценты (0Ц100); false = веса Ч количества</param>
    public static List<T> Get(Dictionary<T, int> weights, int totalCount, bool usePercent = false)
    {
        var pool = new List<T>(totalCount);

        foreach (var pair in weights)
        {
            int count = usePercent
                ? Mathf.RoundToInt((float)pair.Value / 100f * totalCount)
                : pair.Value;

            for (int i = 0; i < count; i++)
            {
                pool.Add(pair.Key);
            }
        }

        return Shuffle(pool);
    }

    /// <summary>
    /// ѕеремешать лист 
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<T> Shuffle(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}

