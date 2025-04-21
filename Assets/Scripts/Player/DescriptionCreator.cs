using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;

public class DescriptionCreator
{
    public class DescriptionVariable : System.Attribute {
        public string color;
        public string detail;

        public DescriptionVariable(string color = "blue", string detail = "")
        {
            this.color = color;
            this.detail = detail;
        }
    }

    public class Variable
    {
        public object value;
        public string detail;
        public string color = "white";
    }

    public static string Generate(string description, Dictionary<string, Variable> variables, bool detailed = false)
    {
        var des = description;
        var words = des.Split(" ");
        string text = "";
        foreach (var word in words)
        {
            if (word.IndexOf("{") < word.IndexOf("}"))
            {
                int indexOfBegin = word.IndexOf("{");
                int indexOfEnd = word.IndexOf("}");
                var variableName = word.Substring(indexOfBegin + 1, indexOfEnd - indexOfBegin - 1).ToLower();
                if (variables.ContainsKey(variableName))
                {
                    text += $"<color={variables[variableName].color}>" + variables[variableName].value + word.Substring(indexOfEnd + 1) + "</color>";
                }
                else
                    text += word;
            }
            else
                text += word;
            text += " ";
        }
        return text.Trim();
    }

    private static string GetDescriptionWithDetails(string description, Dictionary<string, Variable> variables)
    {
        var words = description.Split(" ");
        string newDescription = "";
        foreach (var word in words)
        {
            if (word.IndexOf("{") < word.IndexOf("}"))
            {
                int indexOfBegin = word.IndexOf("{");
                int indexOfEnd = word.IndexOf("}");
                var variableName = word.Substring(indexOfBegin + 1, indexOfEnd - indexOfBegin - 1);
                if (variables.ContainsKey(variableName))
                {
                    if (!String.IsNullOrEmpty(variables[variableName].detail))
                        newDescription += variables[variableName].detail;
                    else
                        newDescription += word;
                }
                else
                    newDescription += word;
            }
            else
                newDescription += word;
            newDescription += " ";
        }
        return newDescription;
    }

    public static string FirstLetterToUpperCase(string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException("There is no first letter");

        char[] a = s.ToCharArray();
        a[0] = char.ToUpper(a[0]);
        return new string(a);
    }


    public static void AddVariablesToDictionary(object obj, Type type, Dictionary<string, Variable> dic)
    {
        var variables = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty).Where(field => field.GetCustomAttribute<DescriptionVariable>() != null).ToList();
        var dictionary = new Dictionary<string, object>();
        foreach (var variable in variables)
        {
            var attribute = variable.GetAttribute<DescriptionVariable>();
            if (dic.ContainsKey(variable.Name.ToLower()))
                continue;
            dic.Add(variable.Name.ToLower(), new Variable() { value = variable.GetValue(obj), color = attribute.color, detail = attribute.detail });
        }
    }

    public static void AddPropertiesToDictionary(object obj, Type type, Dictionary<string, Variable> dic)
    {
        var variables = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetProperty).Where(field => field.GetCustomAttribute<DescriptionVariable>() != null).ToList();
        var dictionary = new Dictionary<string, object>();
        foreach (var variable in variables)
        {
            var attribute = variable.GetAttribute<DescriptionVariable>();
            if (dic.ContainsKey(variable.Name.ToLower()))
                continue;
            dic.Add(variable.Name.ToLower(), new Variable() { value = variable.GetValue(obj), color = attribute.color, detail = attribute.detail });
        }
    }
}
