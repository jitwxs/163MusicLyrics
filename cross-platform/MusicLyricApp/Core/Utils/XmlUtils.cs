using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MusicLyricApp.Core.Utils;

public static partial class XmlUtils
{
    /// <summary>
    /// 创建 XML DOM
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static XmlDocument Create(string content)
    {
        content = NormalizeXmlInput(content);

        content = RemoveIllegalContent(content);

        content = ReplaceAmp(content);

        content = ReplaceQuot(content);

        content = RepairMalformedXml(content);

        var doc = new XmlDocument();

        doc.LoadXml(content);

        return doc;
    }

    private static string NormalizeXmlInput(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        int idx = content.IndexOf("<?xml", System.StringComparison.Ordinal);
        if (idx > 0)
        {
            content = content[idx..];
        }

        return content.TrimStart('\u0000', '\uFEFF');
    }

    private static string ReplaceAmp(string content)
    {
        // replace & symbol
        return AmpRegex().Replace(content, "&amp;");
    }

    private static string ReplaceQuot(string content)
    {
        var sb = new StringBuilder();

        int currentPos = 0;
        foreach (Match match in QuotRegex().Matches(content))
        {
            sb.Append(content.Substring(currentPos, match.Index - currentPos));

            var f = match.Result(match.Groups[1].Value + match.Groups[2].Value.Replace("\"", "&quot;")) + "\"";

            sb.Append(f);

            currentPos = match.Index + match.Length;
        }

        sb.Append(content[currentPos..]);

        return sb.ToString();
    }

    /// <summary>
    /// 移除 XML 内容中无效的部分
    /// </summary>
    /// <param name="content">原始 XML 内容</param>
    /// <returns>移除后的内容</returns>
    private static string RemoveIllegalContent(string content)
    {
        int left = 0, i = 0;
        while (i < content.Length)
        {
            if (content[i] == '<')
            {
                left = i;
            }

            // 闭区间
            if (i > 0 && content[i] == '>' && content[i - 1] == '/')
            {
                var part = content.Substring(left, i - left + 1);

                // 存在有且只有一个等号
                if (part.Contains("=") && part.IndexOf("=") == part.LastIndexOf("="))
                {
                    // 等号和左括号之间没有空格 <a="b" />
                    var part1 = content.Substring(left, part.IndexOf("="));
                    if (!part1.Trim().Contains(" "))
                    {
                        content = content[..left] + content[(i + 1)..];
                        i = 0;
                        continue;
                    }
                }
            }

            i++;
        }

        return content.Trim();
    }
    
    /// <summary>
    /// 对可能含有未转义特殊字符的 XML 字符串进行修复，
    /// 转义标签外文本和属性值内的非法 &lt; 和 &gt;。
    /// 支持 CDATA 和注释跳过处理。
    /// </summary>
    private static string RepairMalformedXml(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return xml;

        var sb = new StringBuilder(xml.Length);
        int len = xml.Length;

        bool insideTag = false;               // 是否在标签内
        bool insideAttributeValue = false;   // 是否在属性值内
        char attributeQuoteChar = '\0';       // 属性值的引号类型
        bool insideCData = false;             // 是否在 <![CDATA[ ]]> 区域
        bool insideComment = false;           // 是否在 <!-- --> 注释区域

        for (int i = 0; i < len; i++)
        {
            char c = xml[i];

            // 检测 CDATA 区域开始
            if (!insideCData && !insideComment && !insideTag && StartsWith(xml, i, "<![CDATA["))
            {
                insideCData = true;
                sb.Append("<![CDATA[");
                i += 8; // 跳过 <![CDATA[
                continue;
            }

            // 检测 CDATA 结束
            if (insideCData)
            {
                if (StartsWith(xml, i, "]]>"))
                {
                    insideCData = false;
                    sb.Append("]]>");
                    i += 2; // 跳过 ]]>
                }
                else
                {
                    // CDATA 内容原样输出，不转义
                    sb.Append(c);
                }
                continue;
            }

            // 检测注释区域开始
            if (!insideComment && !insideCData && !insideTag && StartsWith(xml, i, "<!--"))
            {
                insideComment = true;
                sb.Append("<!--");
                i += 3;
                continue;
            }

            // 检测注释区域结束
            if (insideComment)
            {
                if (StartsWith(xml, i, "-->"))
                {
                    insideComment = false;
                    sb.Append("-->");
                    i += 2;
                }
                else
                {
                    // 注释内容原样输出
                    sb.Append(c);
                }
                continue;
            }

            if (insideTag)
            {
                sb.Append(c);

                // 属性值开始，且非当前属性值时进入属性值模式
                if (!insideAttributeValue && (c == '"' || c == '\''))
                {
                    insideAttributeValue = true;
                    attributeQuoteChar = c;
                }
                // 属性值结束
                else if (insideAttributeValue && c == attributeQuoteChar)
                {
                    insideAttributeValue = false;
                    attributeQuoteChar = '\0';
                }
                else if (insideAttributeValue)
                {
                    // 在属性值内，非法 < 和 > 转义
                    // 但这里不能简单转义所有 < >，因为 < 和 > 可能是正常内容？
                    // 但一般属性值内 < 和 > 应该转义，这里我们转义
                    char prevChar = i > 0 ? xml[i - 1] : '\0';
                    if (c == '<')
                    {
                        // 替换为 &lt;
                        sb.Length--; // 移除刚刚添加的 <
                        sb.Append("&lt;");
                    }
                    else if (c == '>')
                    {
                        sb.Length--;
                        sb.Append("&gt;");
                    }
                }

                if (!insideAttributeValue && c == '>')
                {
                    insideTag = false;
                }
            }
            else
            {
                if (c == '<')
                {
                    insideTag = true;
                    sb.Append(c);
                }
                else
                {
                    // 标签外文本，非法 < 和 > 转义
                    if (c == '<')
                    {
                        sb.Append("&lt;");
                    }
                    else if (c == '>')
                    {
                        sb.Append("&gt;");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static bool StartsWith(string s, int pos, string value)
    {
        if (pos + value.Length > s.Length)
            return false;
        for (int i = 0; i < value.Length; i++)
        {
            if (s[pos + i] != value[i])
                return false;
        }
        return true;
    }

    /// <summary>
    /// 递归查找 XML DOM
    /// </summary>
    /// <param name="xmlNode">根节点</param>
    /// <param name="mappingDict">节点名和结果名的映射</param>
    /// <param name="resDict">结果集</param>
    public static void RecursionFindElement(XmlNode xmlNode, Dictionary<string, string> mappingDict,
        Dictionary<string, XmlNode> resDict)
    {
        if (mappingDict.TryGetValue(xmlNode.Name, out var value))
        {
            resDict[value] = xmlNode;
        }

        if (!xmlNode.HasChildNodes)
        {
            return;
        }

        foreach (XmlNode child in xmlNode.ChildNodes)
        {
            RecursionFindElement(child, mappingDict, resDict);
        }
    }

    [GeneratedRegex("&(?![a-zA-Z]{2,6};|#[0-9]{2,4};)")]
    private static partial Regex AmpRegex();

    [GeneratedRegex(
        "(\\s+[\\w:.-]+\\s*=\\s*\")(([^\"]*)((\")((?!\\s+[\\w:.-]+\\s*=\\s*\"|\\s*(?:/?|\\?)>))[^\"]*)*)\"")]
    private static partial Regex QuotRegex();
}
