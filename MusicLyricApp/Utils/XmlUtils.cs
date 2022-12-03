using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace MusicLyricApp.Utils
{
    public static class XmlUtils
    {
        private static Regex _badAmpersand = new Regex("&(?![a-zA-Z]{2,6};|#[0-9]{2,4};)");
        
        private static string _goodAmpersand = "&amp;";
        
        /// <summary>
        /// 创建 XML DOM
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static XmlDocument Create(string content)
        {
            content = RemoveIllegalContent(content);
            
            // replace & symbol
            content = _badAmpersand.Replace(content, _goodAmpersand);
            
            var doc = new XmlDocument();
            
            doc.LoadXml(content);

            return doc;
        }
        
        /// <summary>
        /// 移除 XML 内容中无效的部分
        /// </summary>
        /// <param name="content">原始 XML 内容</param>
        /// <returns>移除后的内容</returns>
        public static string RemoveIllegalContent(string content)
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
                            content = content.Substring(0, left) + content.Substring(i + 1);
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
        /// 递归查找 XML DOM
        /// </summary>
        /// <param name="xmlNode">根节点</param>
        /// <param name="mappingDict">节点名和结果名的映射</param>
        /// <param name="resDict">结果集</param>
        public static void RecursionFindElement(XmlNode xmlNode, Dictionary<string, string> mappingDict, Dictionary<string, XmlNode> resDict)
        {
            if (mappingDict.TryGetValue(xmlNode.Name, out var value))
            {
                resDict[value] = xmlNode;
            }
           
            if (!xmlNode.HasChildNodes)
            {
                return;
            }
            
            for (var i = 0; i < xmlNode.ChildNodes.Count; i++)
            {
                RecursionFindElement(xmlNode.ChildNodes.Item(i), mappingDict, resDict);
            }
        }
    }
}