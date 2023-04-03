using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

public class AttributeDto
{
    public enum Type
    {
        Android, Tools
    }

    public Type AttrType { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}

public class ManifestHelper
{
    private XDocument doc;
    private XNamespace ns = @"http://schemas.android.com/apk/res/android";
    private XNamespace nsTools = @"http://schemas.android.com/tools";

    public ManifestHelper(string path)
    {
        doc = XDocument.Load(path);
    }

    public ManifestHelper(TextAsset textAsset)
    {
        doc = XDocument.Load(AssetDatabase.GetAssetPath(textAsset));
    }

    public void AddPermission(string permissionValue)
    {
        doc.Root.Add(new XElement("uses-permission", new XAttribute(ns + "name", permissionValue)));
    }

    public void AddMessagingPermission(string bundleIdentifier)
    {
        doc.Root.Add(new XElement("permission", new XAttribute(ns + "name", bundleIdentifier + ".permission.C2D_MESSAGE"),
            new XAttribute(ns + "protectionLevel", "signature")));
    }

    public void Save(string path)
    {
        doc.Save(path);
    }
    public void Save(TextAsset asset)
    {
        doc.Save(AssetDatabase.GetAssetPath(asset));
    }

    public void AddActivity(string name, string theme, bool exported)
    {
        doc.Root.Element("application")
        .Add(new XElement("activity", 
        new XAttribute(ns + "name", name), 
        new XAttribute(ns + "theme", theme), 
        new XAttribute(ns + "exported", exported.ToString().ToLower())));
    }

    public void AddActivity(string name, string theme, bool exported, string configChanges)
    {
        doc.Root.Element("application").Add(new XElement("activity",
            new XAttribute(ns + "name", name),
            new XAttribute(ns + "exported", exported.ToString().ToLower()),
            new XAttribute(ns + "configChanges", configChanges),
            new XAttribute(ns + "theme", theme)));
    }

    public void AddActivity(string name, string theme,bool exported, bool portrait = true)
    {
        doc.Root.Element("application").Add(new XElement("activity",
            new XAttribute(ns + "name", name),
            new XAttribute(ns + "screenOrientation", portrait ? "portrait" : "landscape"),
            new XAttribute(ns + "exported", exported.ToString().ToLower()),
            new XAttribute(ns + "theme", theme)));
    }

    public void AddActivity(string name, bool exported)
    {
        doc.Root.Element("application").Add(new XElement("activity",
            new XAttribute(ns + "name", name),
            new XAttribute(ns + "exported", exported.ToString().ToLower())));
    }

    public void AddMetaData(string name, string value)
    {
        doc.Root.Element("application").Add(new XElement("meta-data",
            new XAttribute(ns + "name", name),
            new XAttribute(ns + "value", value)));
    }

    public void AddToQuerySection_Package(string packageName)
    {
        bool hasQueries = doc.Root.Elements("queries").Any();
        if(!hasQueries)
        {
            doc.Root.Add(new XElement("queries"));
        }
        var queriesElem = doc.Root.Element("queries");
        queriesElem.Add(new XElement("package",new XAttribute(ns+"name",packageName)));
    }

    public void AddToQuerySection_Intent(string intentAction)
    {
        bool hasQueries = doc.Root.Elements("queries").Any();
        if(!hasQueries)
        {
            doc.Root.Add(new XElement("queries"));
        }
        var queriesElem = doc.Root.Element("queries");
        queriesElem.Add(new XElement("intent",
        new XElement("action",new XAttribute(ns+"name",intentAction))));
    }

    public void AddProvider(string name, string authorities, bool exported)
    {
        doc.Root.Element("application").Add(new XElement("provider",
            new XAttribute(ns + "name", name),
            new XAttribute(ns + "authorities", authorities),
            new XAttribute(ns + "exported", exported.ToString().ToLower())
            ));
    }

    public void AddReciever(string name, string actionName,bool exported, XElement data)
    {
        doc.Root.Element("application").Add(new XElement("receiver", new XAttribute(ns + "name", name),
            new XAttribute(ns + "exported", exported.ToString().ToLower()),
            new XElement("intent-filter", new XElement("action", new XAttribute(ns + "name", actionName)), data)
            ));
    }
    public void AddReciever(string recieverName, bool exported, string[] actionNames)
    {
        var actionElements = new List<XElement>();
        foreach (var actionName in actionNames)
        {
            actionElements.Add(new XElement("action", new XAttribute(ns + "name", actionName)));
        }
        doc.Root.Element("application").
            Add(new XElement("receiver",
                    new XAttribute(ns + "name", recieverName), 
                    new XAttribute(ns + "exported", exported.ToString().ToLower()), 
                    new XElement("intent-filter", actionElements)));
    }

    public void AddElementToApplicationTag(XElement element)
    {
        doc.Root.Element("application").Add(element);
    }

    public void AddAttributeToApplicationTag(List<AttributeDto> attributes)
    {
        XElement element = doc.Root.Element("application");

        foreach (var attr in attributes)
        {
            switch (attr.AttrType)
            {
                case AttributeDto.Type.Android:
                    element.Add(new XAttribute(ns + attr.Key, attr.Value));
                    break;
                case AttributeDto.Type.Tools:
                    element.Add(new XAttribute(nsTools + attr.Key, attr.Value));
                    break;
            }
        }
    }

    public void SetVersions(string versionName, int versionCode)
    {
        doc.Root.SetAttributeValue(ns + "versionCode", versionCode);
        doc.Root.SetAttributeValue(ns + "versionName", versionName);
    }
}
