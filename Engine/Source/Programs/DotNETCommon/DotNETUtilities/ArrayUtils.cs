using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tools.DotNETCommon
{
    static class ArrayUtils
    {
        public static bool ByteArraysEqual(byte[] A, byte[] B)
        {
            if (A.Length != B.Length)
            {
                return false;
            }

            for (int Idx = 0; Idx < A.Length; Idx++)
            {
                if (A[Idx] != B[Idx])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
