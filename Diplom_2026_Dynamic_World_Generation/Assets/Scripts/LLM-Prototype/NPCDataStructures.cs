using System;
using UnityEngine;

[Serializable]
public class InputData
{
    public string playerAction;
    public string npcState;
    public ContextData context;
}

[Serializable]
public class ContextData
{
    public string location;
    public string relationship;
}

[Serializable]
public class OutputData
{
    public string dialogue;
    public string action;
    public string emotion;
    public string animation;
}
