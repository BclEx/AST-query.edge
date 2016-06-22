using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class NodeAlter
{
    public string Method { get; private set; }
    public Node Node { get; set; }

    public NodeAlter()
    {
    }

    #region Convert

    public static string ToJson(IEnumerable<NodeAlter> source, bool jsonIndented = false)
    {
        var sb = new StringBuilder();
        var sw = new StringWriter(sb);
        using (var w = new JsonTextWriter(sw))
        {
            if (jsonIndented)
                w.Formatting = Formatting.Indented;
            foreach (var i in source)
                ToJson(i, w);
        }
        return sb.ToString();
    }

    public static IEnumerable<NodeAlter> FromJson(string json)
    {
        var r = new JsonTextReader(new StringReader(json));
        r.Read();
        //
        if (r.TokenType != JsonToken.StartArray) throw new InvalidOperationException("Begin: " + r.TokenType.ToString());
        r.Read();
        var args = new List<NodeAlter>();
        while (r.TokenType != JsonToken.EndArray)
        {
            if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
            else args.Add(FromJson(r));
        }
        r.Read();
        return args;
    }

    private static void ToJson(NodeAlter alter, JsonTextWriter w)
    {
        w.WriteStartObject();
        w.WritePropertyName("name");
        w.WriteValue(alter.Method);
        w.WritePropertyName("ast");
        SyntaxBuilder.ToJsonRecurse(alter.Node, w);
        w.WritePropertyName("bindings"); w.WriteStartObject(); w.WriteEndObject();
        w.WriteEndObject();
    }

    private static NodeAlter FromJson(JsonTextReader r)
    {
        if (r.TokenType != JsonToken.StartObject) throw new InvalidOperationException();
        r.Read(); if ((r.Value as string) != "method" || r.TokenType != JsonToken.PropertyName) throw new InvalidOperationException();
        r.Read(); var method = (r.Value as string);
        r.Read(); if ((r.Value as string) != "ast" || r.TokenType != JsonToken.PropertyName) throw new InvalidOperationException();
        r.Read();
        var node = SyntaxBuilder.FromJsonRecurse(r);
        if ((r.Value as string) == "bindings" && r.TokenType == JsonToken.PropertyName) r.Skip();
        r.Read(); if (r.TokenType != JsonToken.EndObject) throw new InvalidOperationException();
        r.Read();
        return new NodeAlter { Method = method, Node = node };
    }

    #endregion
}
