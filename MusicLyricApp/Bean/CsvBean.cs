using System;
using System.Collections.Generic;
using System.Text;

namespace MusicLyricApp.Bean
{
    public class CsvBean
    {
        public readonly List<string> Title;
        
        public readonly List<List<string>> Lines;

        private int _curLine;

        public CsvBean()
        {
            Title = new List<string>();
            Lines = new List<List<string>>();
        }

        public void AddColumn(string name)
        {
            Title.Add(name);
        }

        public void AddData(string data)
        {
            while (Lines.Count <= _curLine)
            {
                Lines.Add(new List<string>());
            }
            
            Lines[_curLine].Add(data);
        }

        public void NextLine()
        {
            _curLine++;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb
                .Append(string.Join(",", Title))
                .Append(Environment.NewLine);
            
            foreach (var line in Lines)
            {
                sb
                    .Append(string.Join(",", line))
                    .Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public static CsvBean Deserialization(string str)
        {
            var csvBean = new CsvBean();

            try
            {
                var lines = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                foreach (var name in lines[0].Split(','))
                {
                    csvBean.AddColumn(name);
                }

                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    foreach (var data in line.Split(','))
                    {
                        csvBean.AddData(data);
                    }

                    csvBean.NextLine();
                }

                // delete last line
                csvBean._curLine--;
            }
            catch (System.Exception)
            {
                // ignored
            }

            return csvBean;
        }
    }
}