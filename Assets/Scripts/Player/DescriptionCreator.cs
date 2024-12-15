using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DescriptionCreator
{
    public static string Generate(string description, Dictionary<string,object> variables)
    {
        var words = description.Split(" ");
        string text = "";
        foreach (var word in words)
        {
            if (word.IndexOf("{") < word.IndexOf("}"))
            {
                int indexOfBegin = word.IndexOf("{");
                int indexOfEnd = word.IndexOf("}");
                var variableName = word.Substring(indexOfBegin + 1, indexOfEnd - indexOfBegin - 1);
                if (variables.ContainsKey(variableName))
                    text += "<color=red>" + variables[variableName] + word.Substring(indexOfEnd + 1) + "</color>";
                else
                    text += word;
            }
            else
                text += word;
            text += " ";
        }
        return text.Trim();
    }
}
