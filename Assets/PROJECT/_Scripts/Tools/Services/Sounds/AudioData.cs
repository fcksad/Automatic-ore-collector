[System.Serializable]
public class AudioData 
{
    public SerializableDictionary<string, float> SoundVolumes;

    public AudioData()
    {
        SoundVolumes = new SerializableDictionary<string, float>();
    }
}
