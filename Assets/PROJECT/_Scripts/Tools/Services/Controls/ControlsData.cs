[System.Serializable]
public class ControlsData 
{
    public SerializableDictionary<string, string> ControlData;

    public ControlsData()
    {
        ControlData = new SerializableDictionary<string, string>();
    }
}
