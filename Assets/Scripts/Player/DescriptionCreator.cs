using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

public class DescriptionCreator
{
    public class DescriptionVariable : System.Attribute { }

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

    public static string FirstLetterToUpperCase(string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException("There is no first letter");

        char[] a = s.ToCharArray();
        a[0] = char.ToUpper(a[0]);
        return new string(a);
    }


    public static void AddVariablesToDictionary(object obj, Type type, Dictionary<string, object> dic)
    {
        var variables = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty).Where(field => field.GetCustomAttribute<DescriptionVariable>() != null).ToList();
        var dictionary = new Dictionary<string, object>();
        foreach (var variable in variables)
        {
            dic.Add(FirstLetterToUpperCase(variable.Name), variable.GetValue(obj));
        }
    }

    public static void AddPropertiesToDictionary(object obj, Type type, Dictionary<string, object> dic)
    {
        var variables = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty).Where(field => field.GetCustomAttribute<DescriptionVariable>() != null).ToList();
        var dictionary = new Dictionary<string, object>();
        foreach (var variable in variables)
        {
            dic.Add(FirstLetterToUpperCase(variable.Name), variable.GetValue(obj));
        }
    }
}
