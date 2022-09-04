using System;
using System.Collections.Generic;
using System.Text;

namespace MusicLyricApp.Bean
{
    public class CsvBean
    {
        private readonly List<string> title;
        
        private readonly List<List<string>> lines;

        private int curLine;

        public CsvBean()
        {
            title = new List<string>();
            lines = new List<List<string>>();
        }

        public void AddColumn(string name)
        {
            title.Add(name);
        }

        public void AddData(string data)
        {
            while (lines.Count <= curLine)
            {
                lines.Add(new List<string>());
            }
            
            lines[curLine].Add(data);
        }

        public void NextLine()
        {
            curLine++;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb
                .Append(string.Join(",", title))
                .Append(Environment.NewLine);
            
            foreach (var line in lines)
            {
                sb
                    .Append(string.Join(",", line))
                    .Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}