using System.Xml;
using Multiplayer.Common;

namespace Tests;

public class XmlExtensionsTest
{
    private static XmlDocument CreateDoc(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return doc;
    }

    [Test]
    public void AddNode_AddsChildWithText()
    {
        var doc = CreateDoc("<root/>");
        doc.DocumentElement!.AddNode("child", "value");

        var child = doc.DocumentElement["child"];
        Assert.That(child, Is.Not.Null);
        Assert.That(child!.InnerText, Is.EqualTo("value"));
    }

    [Test]
    public void RemoveFromParent_RemovesNode()
    {
        var doc = CreateDoc("<root><child/></root>");
        var child = doc.DocumentElement!["child"]!;

        child.RemoveFromParent();

        Assert.That(doc.DocumentElement["child"], Is.Null);
    }

    [Test]
    public void RemoveFromParent_NullNode_NoOp()
    {
        // Should not throw
        XmlNode? node = null;
        Assert.DoesNotThrow(() => node!.RemoveFromParent());
    }

    [Test]
    public void RemoveChildIfPresent_RemovesExistingChild()
    {
        var doc = CreateDoc("<root><child>text</child></root>");
        doc.DocumentElement!.RemoveChildIfPresent("child");

        Assert.That(doc.DocumentElement["child"], Is.Null);
    }

    [Test]
    public void RemoveChildIfPresent_MissingChild_NoOp()
    {
        var doc = CreateDoc("<root><other/></root>");
        Assert.DoesNotThrow(() => doc.DocumentElement!.RemoveChildIfPresent("child"));
    }

    [Test]
    public void SelectAndRemove_RemovesMatchingNodes()
    {
        var doc = CreateDoc("<root><a/><b/><a/></root>");
        doc.DocumentElement!.SelectAndRemove("a");

        var remaining = doc.DocumentElement.ChildNodes;
        Assert.That(remaining.Count, Is.EqualTo(1));
        Assert.That(remaining[0]!.Name, Is.EqualTo("b"));
    }
}
