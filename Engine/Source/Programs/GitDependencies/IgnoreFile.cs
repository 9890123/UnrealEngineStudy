using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitDependencies
{
    class IgnoreFile
    {
        List<Tuple<Regex, bool>> Patterns = new List<Tuple<Regex, bool>>();

        public IgnoreFile(string FileName) : this(File.ReadAllLines(FileName))
        {

        }

        public IgnoreFile(string[] Lines)
        {
            foreach (string Line in Lines)
            {
                string TrimLine = Line.Trim();
                if (TrimLine.Length > 0 && !TrimLine.StartsWith("#"))
                {
                    bool bExcludeFile = true;
                    if (TrimLine.StartsWith("!"))
                    {
                        TrimLine = TrimLine.Substring(1).TrimStart();
                        bExcludeFile = false;
                    }

                    string FinalPattern = "^" + Regex.Escape(TrimLine.Replace('\\', '/')) + "$";
                    FinalPattern = FinalPattern.Replace("\\?", ".");
                    FinalPattern = FinalPattern.Replace("\\*\\*", ".*");
                    FinalPattern = FinalPattern.Replace("\\*", "[^/]*");

                    if (!FinalPattern.StartsWith("^/"))
                    {
                        FinalPattern = FinalPattern.Substring(1);
                    }
                    if (FinalPattern.EndsWith("/$"))
                    {
                        FinalPattern = FinalPattern.Substring(0, FinalPattern.Length - 1);
                    }

                    Patterns.Add(new Tuple<Regex, bool>(new Regex(FinalPattern, RegexOptions.IgnoreCase), bExcludeFile));
                }
            }
        }

        public bool IsExcludedFile(string FilePath)
        {
            string NormalizedFilePath = FilePath.Replace('\\', '/');
            if (!NormalizedFilePath.StartsWith("/"))
            {
                NormalizedFilePath = "/" + NormalizedFilePath;
            }

            bool bIsExcluded = false;
            foreach (Tuple<Regex, bool> Pattern in Patterns)
            {
                if (bIsExcluded != Pattern.Item2 && Pattern.Item1.IsMatch(NormalizedFilePath))
                {
                    bIsExcluded = Pattern.Item2;
                }
            }
            return bIsExcluded;
        }
    }
}
