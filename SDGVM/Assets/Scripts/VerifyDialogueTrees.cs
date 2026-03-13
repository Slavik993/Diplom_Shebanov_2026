using UnityEngine;
using System.Collections.Generic;

public class VerifyDialogueTrees : MonoBehaviour
{
    void Start()
    {
        Verify();
    }

    public void Verify()
    {
        Debug.Log("Starting Dialogue Trees Verification...");
        
        int[] caseIds = { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
        
        foreach (int id in caseIds)
        {
            DialogueTree tree = DialogueTrees.GetTree(id);
            if (tree == null)
            {
                Debug.LogError($"[Case {id}] Tree is NULL!");
                continue;
            }

            Debug.Log($"[Case {id}] Verifying nodes...");
            VerifyNode(tree, tree.StartNodeId, new HashSet<string>());
        }
        
        Debug.Log("Verification Complete.");
    }

    private void VerifyNode(DialogueTree tree, string nodeId, HashSet<string> visited)
    {
        if (visited.Contains(nodeId)) return;
        visited.Add(nodeId);

        DialogueNode node = tree.GetNode(nodeId);
        if (node == null)
        {
            Debug.LogError($"[Case {tree.CaseId}] Node {nodeId} not found!");
            return;
        }

        if (node.IsEnding)
        {
            return;
        }

        if (node.Choices.Count == 0)
        {
            Debug.LogError($"[Case {tree.CaseId}] Node {nodeId} is NOT an ending but has NO choices!");
            return;
        }

        foreach (var choice in node.Choices)
        {
            VerifyNode(tree, choice.NextNodeId, visited);
        }
    }
}
