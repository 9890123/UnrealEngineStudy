using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.DotNETCommon
{
    [Serializable]
    public abstract class FileSystemReference
    {
        public readonly string FullName;

        public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        public static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

        protected FileSystemReference(string InFullName)
        {
            FullName = InFullName;
        }

        protected FileSystemReference(string InFullName, string InCanonicalName)
        {
            FullName = InFullName;
        }

        static protected string CombineStrings(DirectoryReference BaseDirectory, params string[] Fragments)
        {
            StringBuilder NewFullName = new StringBuilder(BaseDirectory.FullName);
            if (NewFullName.Length > 0 && NewFullName[NewFullName.Length - 1] == Path.DirectorySeparatorChar)
            {
                NewFullName.Remove(NewFullName.Length - 1, 1);
            }

            foreach (string Fragment in Fragments)
            {
                if ((Fragment.Length >= 2 && Fragment[1] == ':') || (Fragment.Length >= 1 && (Fragment[0] == '\\' || Fragment[0] == '/')))
                {
                    NewFullName.Clear();
                    NewFullName.Append(Path.GetFullPath(Fragment).TrimEnd(Path.DirectorySeparatorChar));
                }
                else
                {
                    int StartIdx = 0;
                    while (StartIdx < Fragment.Length)
                    {
                        int EndIdx = StartIdx;
                        while (EndIdx < Fragment.Length && Fragment[EndIdx] != '\\' && Fragment[EndIdx] != '/')
                        {
                            EndIdx++;
                        }

                        int Length = EndIdx - StartIdx;
                        if (Length == 0)
                        {
                            throw new ArgumentException(String.Format("Path fragment '{0}' contains invalid directory separators.", Fragment));
                        }
                        else if (Length == 2 && Fragment[StartIdx] == '.' && Fragment[StartIdx + 1] == '.')
                        {
                            for (int SeparatorIdx = NewFullName.Length - 1; SeparatorIdx >= 0; SeparatorIdx--)
                            {
                                if (NewFullName[SeparatorIdx] == Path.DirectorySeparatorChar)
                                {
                                    NewFullName.Remove(SeparatorIdx, NewFullName.Length - SeparatorIdx);
                                    break;
                                }
                            }
                        }
                        else if (Length != 1 || Fragment[StartIdx] != '.')
                        {
                            NewFullName.Append(Path.DirectorySeparatorChar);
                            NewFullName.Append(Fragment, StartIdx, Length);
                        }

                        StartIdx = EndIdx + 1;
                    }
                }
            }

            if (NewFullName.Length == 0 || (NewFullName.Length == 2 && NewFullName[1] == ':'))
            {
                NewFullName.Append(Path.DirectorySeparatorChar);
            }

            return NewFullName.ToString();
        }

        public bool HasExtension(string Extension)
        {
            if (Extension.Length > 0 && Extension[0] != '.')
            {
                return FullName.Length >= Extension.Length + 1 && FullName[FullName.Length - Extension.Length - 1] == '.' && FullName.EndsWith(Extension, Comparison);
            }
            else
            {
                return FullName.EndsWith(Extension, Comparison);
            }
        }

        public bool IsUnderDirectory(DirectoryReference Other)
        {
            return FullName.StartsWith(Other.FullName, Comparison) && (FullName.Length == Other.FullName.Length || FullName[Other.FullName.Length] == Path.DirectorySeparatorChar || Other.IsRootDirectory());
        }

        public bool ContainsName(string Name, int Offset)
        {
            return ContainsName(Name, Offset, FullName.Length - Offset);
        }

        public bool ContainsName(string Name, int Offset, int Length)
        {
            if (Length < Name.Length)
            {
                return false;
            }

            int MatchIdx = Offset;
            for (; ; )
            {
                MatchIdx = FullName.IndexOf(Name, MatchIdx, Offset + Length - MatchIdx, Comparison);
                if (MatchIdx == -1)
                {
                    return false;
                }

                int MatchEndIdx = MatchIdx + Name.Length;
                if (FullName[MatchIdx - 1] == Path.DirectorySeparatorChar && (MatchEndIdx == FullName.Length || FullName[MatchEndIdx] == Path.DirectorySeparatorChar))
                {
                    return true;
                }

                MatchIdx += Name.Length;
            }
        }

        public bool ContainsName(string Name, DirectoryReference BaseDir)
        {
            if (!IsUnderDirectory(BaseDir))
            {
                return false;
            }
            else
            {
                return ContainsName(Name, BaseDir.FullName.Length);
            }
        }

        public bool ContainsAnyNames(IEnumerable<string> Names, DirectoryReference BaseDir)
        {
            if (!IsUnderDirectory(BaseDir))
            {
                return false;
            }
            else
            {
                return Names.Any(x => ContainsName(x, BaseDir.FullName.Length));
            }
        }

        public string MakeRelativeTo(DirectoryReference Directory)
        {
            int CommonDirectoryLength = -1;
            for (int Idx = 0; ; Idx++)
            {
                if (Idx == FullName.Length)
                {
                    if (Idx == Directory.FullName.Length)
                    {
                        return ".";
                    }

                    if (Directory.FullName[Idx] == Path.DirectorySeparatorChar)
                    {
                        CommonDirectoryLength = Idx;
                    }
                    break;
                }
                else if (Idx == Directory.FullName.Length)
                {
                    if (FullName[Idx] == Path.DirectorySeparatorChar)
                    {
                        CommonDirectoryLength = Idx;
                    }
                    break;
                }
                else
                {
                    if (String.Compare(FullName, Idx, Directory.FullName, Idx, 1, Comparison) != 0)
                    {
                        break;
                    }
                    if (FullName[Idx] == Path.DirectorySeparatorChar)
                    {
                        CommonDirectoryLength = Idx;
                    }
                }
            }

            if (CommonDirectoryLength == -1)
            {
                return FullName;
            }

            StringBuilder Result = new StringBuilder();
            for (int Idx = CommonDirectoryLength + 1; Idx < Directory.FullName.Length; Idx++)
            {
                Result.Append("..");
                Result.Append(Path.DirectorySeparatorChar);

                while (Idx < Directory.FullName.Length && Directory.FullName[Idx] != Path.DirectorySeparatorChar)
                {
                    Idx++;
                }
            }
            if (CommonDirectoryLength + 1 < FullName.Length)
            {
                Result.Append(FullName, CommonDirectoryLength + 1, FullName.Length - CommonDirectoryLength - 1);
            }

            return Result.ToString();
        }

        public string ToNormalizedPath()
        {
            return FullName.Replace("\\", "/");
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
