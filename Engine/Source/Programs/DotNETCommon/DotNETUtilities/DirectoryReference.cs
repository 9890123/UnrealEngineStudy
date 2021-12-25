using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.DotNETCommon
{
    [Serializable]
    public class DirectoryReference : FileSystemReference, IEquatable<DirectoryReference>
    {
        public enum Sanitize
        {
            None
        }

        public DirectoryReference(string InPath)
            : base(FixTrailingPathSeparator(Path.GetFullPath(InPath)))
        {

        }

        public DirectoryReference(DirectoryInfo InInfo)
            : base(FixTrailingPathSeparator(InInfo.FullName))
        {

        }

        public DirectoryReference(string InFullName, Sanitize InSanitize)
            : base(InFullName)
        { 
        }

        private static string FixTrailingPathSeparator(string DirName)
        {
            if (DirName.Length == 2 && DirName[1] == ':')
            {
                return DirName + Path.DirectorySeparatorChar;
            }
            else if (DirName.Length == 3 && DirName[1] == ':' && DirName[2] == Path.DirectorySeparatorChar)
            {
                return DirName;
            }
            else if (DirName.Length > 1 && DirName[DirName.Length - 1] == Path.DirectorySeparatorChar)
            {
                return DirName.TrimEnd(Path.DirectorySeparatorChar);
            }
            else
            {
                return DirName;
            }
        }

        public string GetDirectoryName()
        {
            return Path.GetFileName(FullName);
        }

        public DirectoryReference ParentDirectory
        {
            get
            {
                if (IsRootDirectory())
                {
                    return null;
                }

                int ParentLength = FullName.LastIndexOf(Path.DirectorySeparatorChar);
                if (ParentLength == 2 && FullName[1] == ':')
                {
                    ParentLength++;
                }

                if (ParentLength == 0 && FullName[0] == Path.DirectorySeparatorChar)
                {
                    ParentLength = 1;
                }

                return new DirectoryReference(FullName.Substring(0, ParentLength), Sanitize.None);
            }
        }

        [Obsolete("Replace with call to FileReference.ParentDirectory instead.")]
        public static DirectoryReference GetParentDiretory(FileReference File)
        {
            int ParentLength = File.FullName.LastIndexOf(Path.DirectorySeparatorChar);
            if (ParentLength == 2 && File.FullName[1] == ':')
            {
                ParentLength++;
            }

            return new DirectoryReference(File.FullName.Substring(0, ParentLength), Sanitize.None);
        }

        public static DirectoryReference GetSpecialFolder(Environment.SpecialFolder Folder)
        {
            string FolderPath = Environment.GetFolderPath(Folder);
            return String.IsNullOrEmpty(FolderPath) ? null : new DirectoryReference(FolderPath);
        }

        public bool IsRootDirectory()
        {
            return FileName[FullName.Length - 1] == Path.DirectorySeparatorChar;
        }

        public static DirectoryReference Combine(DirectoryReference BaseDirectory, params string[] Fragments)
        {
            string FullName = FileSystemReference.CombineStrings(BaseDirectory, Fragments);
            return new DirectoryReference(FullName, Sanitize.None);
        }

        public static bool operator ==(DirectoryReference A, DirectoryReference B)
        {
            if ((object)A == null)
            {
                return (object)B == null;
            }
            else
            {
                return (object)B != null && A.FullName.Equals(B.FullName, Comparison);
            }
        }

        public static bool operator !=(DirectoryReference A, DirectoryReference B)
        {
            return !(A == B);
        }

        public override bool Equals(object Obj)
        {
            return (Obj is DirectoryReference) && ((DirectoryReference)Obj) == this;
        }

        public bool Equals(DirectoryReference Obj)
        {
            return Obj == this;
        }

        public override int GetHashCode()
        {
            return Comparer.GetHashCode(FullName);
        }

        public static DirectoryReference MakeRemote(string AbsolutePath)
        {
            return new DirectoryReference(AbsolutePath, Sanitize.None);
        }

        public static DirectoryReference FromFile(FileReference File)
        {
            if (File == null)
            {
                return null;
            }
            else
            {
                return File.Directory;
            }
        }

        public static DirectoryReference FromString(string DirectoryName)
        {
            if (String.IsNullOrEmpty(DirectoryName))
            {
                return null;
            }
            else
            {
                return new DirectoryReference(DirectoryName);
            }
        }

        public static DirectoryReference FindCorrectCase(DirectoryReference Location)
        {
            return new DirectoryReference(DirectoryUtils.FindCorrectCase(Location.ToDirectoryInfo()));
        }

        public DirectoryInfo ToDirectoryInfo()
        {
            return new DirectoryInfo(FullName);
        }

        #region System.IO.Directory Wrapper Methods
        
        public static DirectoryReference GetCurrentDirectory()
        {
            return new DirectoryReference(Directory.GetCurrentDirectory());
        }

        public static void CreateDirectory(DirectoryReference Location)
        {
            Directory.CreateDirectory(Location.FullName);
        }

        public static void Delete(DirectoryReference Location)
        {
            Directory.Delete(Location.FullName);
        }

        public static void Delete(DirectoryReference Location, bool bRecursive)
        {
            Directory.Delete(Location.FullName, bRecursive);
        }

        public static bool Exists(DirectoryReference Location)
        {
            return Directory.Exists(Location.FullName);
        }

        public static IEnumerable<FillReference> EnumerateFiles(DirectoryReference BaseDir)
        {
            foreach (string FileName in Directory.EnumerateFiles(BaseDir.FullName))
            {
                yield return new FileReference(FileName, FileReference.Sanitize.None);
            }
        }
        
        public static IEnumerable<FileReference> EnumerateFiles(DirectoryReference BaseDir, string Pattern)
        {
            foreach (string FileName in Directory.EnumerateFiles(BaseDir.FullName, Pattern))
            {
                yield return new FileReference(FileName, FileReference.Sanitize.None);
            }
        }

        public static IEnumerable<FileReference> EnumerateFiles(DirectoryReference BaseDir, string Pattern, SearchOption Option)
        {
            foreach (string FileName in Directory.EnumerateFiles(BaseDir.FullName, Pattern, Option))
            {
                yield return new FileReference(FileName, FileReference.Sanitize.None);
            }
        }

        public static IEnumerable<DirectoryReference> EnumerateDirectories(DirectoryReference BaseDir)
        {
            foreach (string DirectoryName in Directory.EnumerateDirectories(BaseDir.FullName))
            {
                yield return new DirectoryReference(DirectoryName, Sanitize.None);
            }
        }

        public static IEnumerable<DirectoryReference> EnumerateDirectories(DirectoryReference BaseDir, string Pattern)
        {
            foreach (string DirectoryName in Directory.EnumerateDirectories(BaseDir.FullName, Pattern))
            {
                yield return new DirectoryReference(DirectoryName, Sanitize.None);
            }
        }

        public static IEnumerable<DirectoryReference> EnumerateDirectories(DirectoryReference BaseDir, string Pattern, SearchOption Option)
        {
            foreach (string DirectoryName in Directory.EnumerateDirectories(BaseDir.FullName, Pattern, Option))
            {
                yield return new DirectoryReference(DirectoryName, Sanitize.None);
            }
        }

        public static void SetCurrentDirectory(DirectoryReference Location)
        {
            Directory.SetCurrentDirectory(Location.FullName);
        }

        #endregion
    }

    public static class DirectoryReferenceExtensionMethods
    {
        public static void Write(this BinaryWriter Writer, DirectoryReference Directory)
        {
            Writer.Write((Directory == null) ? String.Empty : Directory.FullName);
        }

        public static DirectoryReference ReadDirectoryReference(this BinaryReader Reader)
        {
            string FullName = Reader.ReadString();
            return (FullName.Length == 0) ? null : new DirectoryReference(FullName, DirectoryReference.Sanitize.None);
        }

        public static void WriteDirectoryReference(this BinaryArchiveWriter Writer, DirectoryReference Directory)
        {
            if (Directory == null)
            {
                Writer.WriteString(null);
            }
            else
            {
                Writer.WriteString(Directory.FullName);
            }
        }

        public static DirectoryReference ReadDirectoryReference(this BinaryArchiveReader Reader)
        {
            string FullName = Reader.ReadString();
            if (FullName == null)
            {
                return null;
            }
            else
            {
                return new DirectoryReference(FullName, DirectoryReference.Sanitize.None);
            }
        }
    }
}
