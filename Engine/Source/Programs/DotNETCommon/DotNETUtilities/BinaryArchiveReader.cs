using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.DotNETCommon
{
    public class BinaryArchiveReader : IDisposable
    {
        Stream Stream;
        byte[] Buffer;
        int BufferPos;
        List<object> Objects = new List<object>();

        public BinaryArchiveReader(byte[] Buffer)
        {
            this.Buffer = Buffer;
        }

        public BinaryArchiveReader(FileReference FileName)
        {
            this.Buffer = FileReference.ReadAllBytes(FileName);
        }

        public BinaryArchiveReader(Stream Stream)
        {
            Buffer = new byte[Stream.Length];
            Stream.Read(Buffer, 0, Buffer.Length);
        }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            Buffer = null;
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public byte ReadByte()
        {
            byte Value = Buffer[BufferPos];
            BufferPos++;
            return Value;
        }

        public sbyte ReadSignedByte()
        {
            return (sbyte)ReadByte();
        }

        public short ReadShort()
        {
            return (short)ReadUnsignedShort();
        }

        public ushort ReadUnsignedShort()
        {
            ushort Value = (ushort)(Buffer[BufferPos + 0] | (Buffer[BufferPos + 1] << 8));
            BufferPos += 2;
            return Value;
        }

        public int ReadInt()
        {
            return (int)ReadUnsignedInt();
        }

        public uint ReadUnsignedInt()
        {
            uint Value = (uint)(Buffer[BufferPos + 0] | (Buffer[BufferPos + 1] << 8) | (Buffer[BufferPos + 2] << 16) | (Buffer[BufferPos + 3] << 24));
            BufferPos += 4;
            return Value;
        }

        public long ReadLong()
        {
            return (long)ReadUnsignedLong();
        }

        public ulong ReadUnsignedLong()
        {
            ulong Value = (ulong)ReadUnsignedInt();
            Value |= (ulong)ReadUnsignedInt() << 32;
            return Value;
        }

        public string ReadString()
        {
            byte[] Bytes = ReadByteArray();
            if (Bytes == null)
            {
                return null;
            }
            else
            {
                return Encoding.UTF8.GetString(Bytes);
            }
        }

        public byte[] ReadByteArray()
        {
            return ReadPrimitiveArray<byte>(sizeof(byte));
        }

        public short[] ReadShortArray()
        {
            return ReadPrimitiveArray<short>(sizeof(short));
        }

        public int[] ReadIntArray()
        {
            return ReadPrimitiveArray<int>(sizeof(int));
        }

        private T[] ReadPrimitiveArray<T>(int ElementSize) where T : struct
        {
            int Length = ReadInt();
            if (Length < 0)
            {
                return null;
            }
            else
            {
                T[] Result = new T[Length];
                ReadBulkData(Result, Length * ElementSize);
                return Result;
            }
        }

        public byte[] ReadFixedSizeByteArray(int Length)
        {
            return ReadFixedSizePrimitiveArray<byte>(sizeof(byte), Length);
        }

        public short[] ReadFixedSizeShortArray(int Length)
        {
            return ReadFixedSizePrimitiveArray<short>(sizeof(short), Length);
        }

        public int[] ReadFixedSizeIntArray(int Length)
        {
            return ReadFixedSizePrimitiveArray<int>(sizeof(int), Length);
        }

        private T[] ReadFixedSizePrimitiveArray<T>(int ElementSize, int ElementCount) where T : struct
        {
            T[] Result = new T[ElementCount];
            ReadBulkData(Result, ElementSize * ElementCount);
            return Result;
        }

        private void ReadBulkData(Array Data, int Size)
        {
            System.Buffer.BlockCopy(Buffer, BufferPos, Data, 0, Size);
            BufferPos += Size;
        }

        public T[] ReadArray<T>(Func<T> ReadElement)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }
            else
            {
                T[] Result = new T[Count];
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result[Idx] = ReadElement();
                }
                return Result;
            }
        }

        public List<T> ReadList<T>(Func<T> ReadElement)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }
            else
            {
                List<T> Result = new List<T>(Count);
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result.Add(ReadElement());
                }
                return Result;
            }
        }

        public HashSet<T> ReadHashSet<T>(Func<T> ReadElement)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }
            else
            {
                HashSet<T> Result = new HashSet<T>();
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result.Add(ReadElement());
                }
                return Result;
            }
        }

        public HashSet<T> ReadHashSet<T>(Func<T> ReadElement, IEqualityComparer<T> Comparer)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }
            else
            {
                HashSet<T> Result = new HashSet<T>(Comparer);
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result.Add(ReadElement());
                }
                return Result;
            }
        }

        public Dictionary<K, V> ReadDictionary<K, V>(Func<K> ReadKey, Func<V> ReadValue)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }else
            {
                Dictionary<K, V> Result = new Dictionary<K, V>(Count);
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result.Add(ReadKey(), ReadValue());
                }
                return Result;
            }
        }

        public Dictionary<K, V> ReadDictionary<K, V>(Func<K> ReadKey, Func<V> ReadValue, IEqualityComparer<K> Comparer)
        {
            int Count = ReadInt();
            if (Count < 0)
            {
                return null;
            }
            else
            {
                Dictionary<K, V> Result = new Dictionary<K, V>(Count, Comparer);
                for (int Idx = 0; Idx < Count; Idx++)
                {
                    Result.Add(ReadKey(), ReadValue());
                }
                return Result;
            }
        }

        public Nullable<T> ReadNullable<T>(Func<T> ReadValue) where T : struct
        {
            if (ReadBool())
            {
                return new Nullable<T>(ReadValue());
            }
            else
            {
                return null;
            }
        }

        public T ReadOptionalObject<T>(Func<T> Read) where T : class
        {
            if (ReadBool())
            {
                return Read();
            }
            else
            {
                return null;
            }
        }

        public T ReadObjectReference<T>(Func<T> CreateObject, Action<T> ReadObject) where T : class
        {
            int Index = ReadInt();
            if (Index < 0)
            {
                return null;
            }
            else
            {
                if (Index == Objects.Count)
                {
                    T Object = CreateObject();
                    Objects.Add(Object);
                    ReadObject(Object);
                }
                return (T)Objects[Index];
            }
        }

        public T ReadObjectReference<T>(Func<T> ReadObject) where T : class
        {
            int Index = ReadInt();
            if (Index < 0)
            {
                return null;
            }
            else
            {
                if (Index == Objects.Count)
                {
                    Objects.Add(null);
                    Objects[Index] = ReadObject();
                }
                if (Objects[Index] == null)
                {
                    throw new InvalidOperationException("Attempt to serialize reference to object recursively.");
                }
                return (T)Objects[Index];
            }
        }
    }
}
