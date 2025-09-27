using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomAnimClip", menuName = "CustomAnimClip/Clip")]
public class CustomAnimClip : ScriptableObject
{
    [Serializable]
    public struct Event
    {
        public float time;         
        public string name;
    }

    [Serializable]
    public struct Frame
    {
        public Vector3[] LocalPos;  
        public Quaternion[] LocalRot;
        public Vector3[] LocalScale;
    }

    [Tooltip("»мена/пути костей относительно корн€ рига")]
    public string[] BonePaths;

    [Tooltip(" адры клипа (равномерна€ сетка времени)")]
    public Frame[] Frames;

    [Min(1)] public int Fps = 30;
    public float Length = 1f;       
    public bool Loop = true;

    [Tooltip("ќпциональные событи€, срабатывают при проходе времени через time")]
    public CustimAnimEvent[] Events;

    public int FrameCount => Frames != null ? Frames.Length : 0;
    public float FrameDelta => 1f / Mathf.Max(1, Fps);

    public float ClampTime(float t)
    {
        if (Length <= 0f) return 0f;
        if (Loop) return Mathf.Repeat(t, Length);
        return Mathf.Clamp(t, 0f, Length - 1e-6f);
    }
}

[System.Serializable]
public struct CustimAnimEvent
{
    public string Name;
    [Range(0, 1f)] public float NormalizedTime; 
}
