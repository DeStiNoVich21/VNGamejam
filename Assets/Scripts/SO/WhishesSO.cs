using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "WhishConfig")]
public class WhishesSO : SerializedScriptableObject                    
{
    public Whish[] whishes;
}

public class Whish
{
    [TextArea] public string whishInfo;
    public BetterEvent OnAgree = new(), OnDisagree = new();
}
