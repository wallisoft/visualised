using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VB;

public class YamlLoader
{
    public static Dictionary<string, object> Load(string filePath)
    {
        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        
        return deserializer.Deserialize<Dictionary<string, object>>(yaml);
    }
}

