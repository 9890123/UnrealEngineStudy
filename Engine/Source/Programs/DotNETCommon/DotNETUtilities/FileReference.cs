using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.DotNETCommon
{
    [Serializable]
    public class FileReference: FileSystemReference, IEquatable<FileReference>
    {
        public enum Sanitize
        {
            None
        }

        public FileReference(string InPath)
            : base(Path.GetFullPath(InPath))
        {
            if (FullName[FullName.Length - 1] == '\\' || FullName[FullName.Length - 1] == '/')
            {
                throw new ArgumentException("File names may not be terminated by a path separator character");
            }
        }

        public FileReference(FileInfo InInfo)
            : base(InInfo.FullName)
        {

        }

        public FileReference(string InFullName, Sanitize InSanitize)
            : base(InFullName)
        {

        }

        public static FileReference FromString(string FileName)
        {
            if (String.IsNullOrEmpty(FileName))
            {
                return null;
            }
            else
            {
                return new FileReference(FileName);
            }
        }

        public string GetFileName()
        {
            return Path.GetFileName(FullName);
        }

        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(FullName);
        }

        public string GetFileNameWithoutAnyExtensions()
        {
            int StartIdx = FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1;

            int EndIdx = FullName.IndexOf('.', StartIdx);
            if (EndIdx < StartIdx)
            {
                return FullName.Substring(StartIdx);
            }
            else
            {
                return FullName.Substring(StartIdx, EndIdx - StartIdx);
            }
        }

        public string GetExtension()
        {
            return Path.GetExtension(FullName);
        }

        public FileReference ChangeExtension(string Extension)
        {
            string NewFullName = Path.ChangeExtension(FullName, Extension);
            return new FileReference(NewFullName, Sanitize.None);
        }

        public DirectoryReference Directory
        {
            get
            {
                int ParentLength = FullName.LastIndexOf(Path.DirectorySeparatorChar);

                if (ParentLength == 2 && FullName[1] == ':')
                {
                    ParentLength++;
                }

                if (ParentLength == 0 && FullName[0] == Path.DirectorySeparatorChar)
                {
                    ParentLength = 1;
                }

                return new DirectoryReference(FullName.Substring(0, ParentLength), DirectoryReference.Sanitize.None);
            }
        }

        public static FileReference Combine(DirectoryReference BaseDirectory, params string[] Fragments)
        {
            string FullName = FileSystemReference.CombineStrings(BaseDirectory, Fragments);
            return new FileReference(FullName, Sanitize.None);
        }

        public static FileReference operator +(FileReference A, string B)
        {
            return new FileReference(A.FullName + B, Sanitize.None);
        }

        public static bool operator ==(FileReference A, FileReference B)
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

        public static bool operator !=(FileReference A, FileReference B)
        {
            return !(A == B);
        }

        public override bool Equals(object Obj)
        {
            return (Obj is FileReference) && ((FileReference)Obj) == this;
        }

        public bool Equals(FileReference Obj)
        {
            return Obj == this;
        }

        public override int GetHashCode()
        {
            return Comparer.GetHashCode(FullName);
        }

        public static FileReference MakeRemote(string AbsolutePath)
        {
            return new FileReference(AbsolutePath, Sanitize.None);
        }

        public static void MakeWriteable(FileReference Location)
        {
            if (Exists(Location))
            {
                FileAttributes Attributes = GetAttributes(Location);
                if ((Attributes & FileAttributes.ReadOnly) != 0)
                {
                    SetAttributes(Location, Attributes & ~FileAttributes.ReadOnly);
                }
            }
        }

        public static FileReference FindCorrectCase(FileReference Location)
        {
            return new FileReference(FileUtils.FindCorrectCase(Location.ToFileInfo()));
        }

        public FileInfo ToFileInfo()
        {
            return new FileInfo(FullName);
        }

        #region System.IO.File methods
        
        public static void Copy(FileReference SourceLocation, FileReference TargetLocation)
        {
            File.Copy(SourceLocation.FullName, TargetLocation.FullName);
        }

        public static void Copy(FileReference SourceLocation, FileReference TargetLocation, bool bOverwrite)
        {
            File.Copy(SourceLocation.FullName, TargetLocation.FullName, bOverwrite);
        }

        public static void Delete(FileReference Location)
        {
            File.Delete(Location.FullName);
        }

        public static bool Exists(FileReference Location)
        {
            return File.Exists(Location.FullName);
        }

        public static FileAttributes GetAttributes(FileReference Location)
        {
            return File.GetAttributes(Location.FullName);
        }

        public static DateTime GetLastWriteTime(FileReference Location)
        {
            return File.GetLastWriteTime(Location.FullName);
        }

        public static DateTime GetLastWriteTimeUtc(FileReference Location)
        {
            return File.GetLastWriteTimeUtc(Location.FullName);
        }

        public static void Move(FileReference SourceLocation, FileReference TargetLocation)
        {
            File.Move(SourceLocation.FullName, TargetLocation.FullName);
        }

        public static FileStream Open(FileReference Location, FileMode Mode)
        {
            return File.Open(Location.FullName, Mode);
        }

        public static FileStream Open(FileReference Location, FileMode Mode, FileAccess Access)
        {
            return File.Open(Location.FullName, Mode, Access);
        }

        public static FileStream Open(FileReference Location, FileMode Mode, FileAccess Access, FileShare Share)
        {
            return File.Open(Location.FullName, Mode, Access, Share);
        }

        public static byte[] ReadAllBytes(FileReference Location)
        {
            return File.ReadAllBytes(Location.FullName);
        }

        public static string ReadAllText(FileReference Location)
        {
            return File.ReadAllText(Location.FullName);
        }

        public static string ReadAllText(FileReference Location, Encoding Encoding)
        {
            return File.ReadAllText(Location.FullName, Encoding);
        }

        public static string[] ReadAllLines(FileReference Location)
        {
            return File.ReadAllLines(Location.FullName);
        }

        public static string[] ReadAllLines(FileReference Location, Encoding Encoding)
        {
            return File.ReadAllLines(Location.FullName, Encoding);
        }

        public static void SetAttributes(FileReference Location, FileAttributes Attributes)
        {
            File.SetAttributes(Location.FullName, Attributes);
        }

        public static void SetLastWriteTime(FileReference Location, DateTime LastWriteTime)
        {
            File.SetLastWriteTime(Location.FullName, LastWriteTime);
        }

        public static void SetLastWriteTimeUtc(FileReference Location, DateTime LastWriteTimeUtc)
        {
            File.SetLastWriteTimeUtc(Location.FullName, LastWriteTimeUtc);
        }

        public static void SetLastAccessTime(FileReference Location, DateTime LastWriteTime)
        {
            File.SetLastWriteTime(Location.FullName, LastWriteTime);
        }

        public static void SetLastAccessTimeUtc(FileReference Location, DateTime LastWriteTimeUtc)
        {
            File.SetLastWriteTimeUtc(Location.FullName, LastWriteTimeUtc);
        }

        public static void WriteAllBytes(FileReference Location, byte[] Contents)
        {
            File.WriteAllBytes(Location.FullName, Contents);
        }

        public static void WriteAllBytesIfDifferent(FileReference Location, byte[] Contents)
        {
            if (FileReference.Exists(Location))
            {
                byte[] CurrentContents = FileReference.ReadAllBytes(Location);
                if (ArrayUtils.ByteArraysEqual(Contents, CurrentContents))
                {
                    return;
                }
            }
            WriteAllBytes(Location, Contents);
        }

        public static void WriteAllLines(FileReference Location, IEnumerable<string> Contents)
        {
            File.WriteAllLines(Location.FullName, Contents);
        }

        public static void WriteAllLines(FileReference Location, IEnumerable<string> Contents, Encoding Encoding)
        {
            File.WriteAllLines(Location.FullName, Contents, Encoding);
        }

        public static void WriteAllLines(FileReference Location, string[] Contents)
        {
            File.WriteAllLines(Location.FullName, Contents);
        }

        public static void WriteAllText(FileReference Location, string[] Contents, Encoding Encoding)
        {
            File.WriteAllLines(Location.FullName, Contents, Encoding);
        }

        public static void WriteAllText(FileReference Location, string Contents)
        {
            File.WriteAllText(Location.FullName, Contents);
        }

        public static void WriteAllText(FileReference Location, string Contents, Encoding Encoding)
        {
            File.WriteAllText(Location.FullName, Contents, Encoding);
        }

        public static void AppendAllLines(FileReference Location, IEnumerable<string> Contents)
        {
            File.AppendAllLines(Location.FullName, Contents);
        }

        public static void AppendAllLines(FileReference Location, IEnumerable<string> Contents, Encoding Encoding)
        {
            File.AppendAllLines(Location.FullName, Contents, Encoding);
        }

        public static void AppendAllLines(FileReference Location, string[] Contents)
        {
            File.AppendAllLines(Location.FullName, Contents);
        }

        public static void AppendAllLines(FileReference Location, string[] Contents, Encoding Encoding)
        {
            File.AppendAllLines(Location.FullName, Contents, Encoding);
        }

        public static void AppendAllText(FileReference Location, string Contents)
        {
            File.AppendAllText(Location.FullName, Contents);
        }

        public static void AppendAllText(FileReference Location, string Contents, Encoding Encoding)
        {
            File.AppendAllText(Location.FullName, Contents, Encoding);
        }

        #endregion
    }

    public static class FileReferenceExtensionMethods
    {
        public static void Write(this BinaryWriter Writer, FileReference File)
        {
            Writer.Write((File == null) ? String.Empty : File.FullName);
        }

        public static void Write(this BinaryWriter Writer, FileReference File, Dictionary<FileReference, int> FileToUniqueId)
        {
            int UniqueId;
            if (File == null)
            {
                Writer.Write(-1);
            }
            else if (FileToUniqueId.TryGetValue(File, out UniqueId))
            {
                Writer.Write(UniqueId);
            }
            else
            {
                Writer.Write(FileToUniqueId.Count);
                Writer.Write(File);
                FileToUniqueId.Add(File, FileToUniqueId.Count);
            }
        }

        public static FileReference ReadFileReference(this BinaryReader Reader)
        {
            string FullName = Reader.ReadString();
            return (FullName.Length == 0) ? null : new FileReference(FullName, FileReference.Sanitize.None);
        }

        public static FileReference ReadFileReference(this BinaryReader Reader, List<FileReference> UniqueFiles)
        {
            int UniqueId = Reader.ReadInt32();
            if (UniqueId == -1)
            {
                return null;
            }
            else if (UniqueId < UniqueFiles.Count)
            {
                return UniqueFiles[UniqueId];
            }
            else
            {
                FileReference Result = Reader.ReadFileReference();
                UniqueFiles.Add(Result);
                return Result;
            }
        }
         
        public static void WriteFileReference(this BinaryArchiveWriter Writer, FileReference File)
        {
            if (File == null)
            {
                Writer.WriteString(null);
            }
            else
            {
                Writer.WriteString(File.FullName);
            }
        }

        public static FileReference ReadFileReference(this BinaryArchiveReader Reader)
        {
            string FullName = Reader.ReadString();
            if (FullName == null)
            {
                return null;
            }
            else
            {
                return new FileReference(FullName, FileReference.Sanitize.None);
            }
        }
    }
}
