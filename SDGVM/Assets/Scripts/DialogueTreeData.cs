using System;
using System.Collections.Generic;

/// <summary>
/// Вариант ответа игрока в диалоговом дереве
/// </summary>
[Serializable]
public class PlayerChoice
{
    public string Text;           // Текст кнопки выбора
    public string NextNodeId;     // ID следующего узла диалога
    public float HumanityBonus;   // Влияние на HI (+0.3 эмпатия, -0.1 агрессия)
    
    public PlayerChoice(string text, string nextNodeId, float humanityBonus = 0.1f)
    {
        Text = text;
        NextNodeId = nextNodeId;
        HumanityBonus = humanityBonus;
    }
}

/// <summary>
/// Узел диалогового дерева (реплика NPC или системное сообщение)
/// </summary>
[Serializable]
public class DialogueNode
{
    public string NodeId;                 // Уникальный ID узла
    public string Speaker;                // "NPC" или "Система"
    public string Text;                   // Текст реплики
    public string Emotion;                // Эмоция NPC в этом узле
    public List<PlayerChoice> Choices;    // Варианты ответа игрока
    public bool IsEnding;                 // Финальный узел (конец сценария)

    public DialogueNode(string nodeId, string text, string emotion = "", bool isEnding = false)
    {
        NodeId = nodeId;
        Speaker = "NPC";
        Text = text;
        Emotion = emotion;
        Choices = new List<PlayerChoice>();
        IsEnding = isEnding;
    }

    public DialogueNode AddChoice(string text, string nextNodeId, float humanityBonus = 0.1f)
    {
        Choices.Add(new PlayerChoice(text, nextNodeId, humanityBonus));
        return this;
    }
}

/// <summary>
/// Полное диалоговое дерево для одного проблемного кейса
/// </summary>
[Serializable]
public class DialogueTree
{
    public int CaseId;                                // Ссылка на AdaptationCase.Id
    public string StartNodeId;                        // ID стартового узла
    public Dictionary<string, DialogueNode> Nodes;    // Все узлы по ID

    public DialogueTree(int caseId, string startNodeId)
    {
        CaseId = caseId;
        StartNodeId = startNodeId;
        Nodes = new Dictionary<string, DialogueNode>();
    }

    public DialogueTree AddNode(DialogueNode node)
    {
        Nodes[node.NodeId] = node;
        return this;
    }

    public DialogueNode GetNode(string nodeId)
    {
        return Nodes.ContainsKey(nodeId) ? Nodes[nodeId] : null;
    }

    public DialogueNode GetStartNode()
    {
        return GetNode(StartNodeId);
    }
}
