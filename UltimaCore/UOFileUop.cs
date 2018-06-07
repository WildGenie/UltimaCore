﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UltimaCore
{
    public class UOFileUop : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;

        private readonly string _extension;
        private readonly int _count;

        public UOFileUop(string path, string extension, int count) : base(path)
        {
             _extension = extension; _count = count;

            Load();
        }

        protected override void Load()
        {
            base.Load();

            Seek(0);

            if (ReadUInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");
            Skip(8);
            long nextBlock = ReadLong();
            Skip(4);

            int count = ReadInt();

            Entries = new UOFileIndex3D[_count];
            Dictionary<ulong, int> hashes = new Dictionary<ulong, int>();

            string pattern = System.IO.Path.GetFileNameWithoutExtension(FileName).ToLowerInvariant();

            for (int i = 0; i < _count; i++)
            {
                string file = string.Format("build/{0}/{1:D8}{2}", pattern, i, _extension);
                ulong hash = CreateHash(file);
                if (!hashes.ContainsKey(hash))
                    hashes.Add(hash, i);
            }

            Seek(nextBlock);

            do
            {
                int filesCount = ReadInt();
                nextBlock = ReadLong();

                for (int i = 0; i < filesCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(4);
                    short flag = ReadShort();

                    int length = flag == 1 ? compressedLength : decompressedLength;
                    if (offset == 0)
                        continue;

                    if (hashes.TryGetValue(hash, out int idx))
                    {
                        if (idx < 0 || idx > Entries.Length)
                            throw new IndexOutOfRangeException("hashes dictionary and files collection have different count of entries!");
                        Entries[idx] = new UOFileIndex3D(offset + headerLength, length);

                        // extra?
                    }
                    else
                       throw new ArgumentException(string.Format("File with hash 0x{0:X8} was not found in hashes dictionary! EA Mythic changed UOP format!", hash));
                }
                Seek(nextBlock);
            } while (nextBlock != 0);
        }

        internal static ulong CreateHash(string s)
        {
            uint eax, ecx, edx, ebx, esi, edi;

            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint)s.Length + 0xDEADBEEF;

            int i = 0;

            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint)((s[i + 7] << 24) | (s[i + 6] << 16) | (s[i + 5] << 8) | s[i + 4]) + edi;
                esi = (uint)((s[i + 11] << 24) | (s[i + 10] << 16) | (s[i + 9] << 8) | s[i + 8]) + esi;
                edx = (uint)((s[i + 3] << 24) | (s[i + 2] << 16) | (s[i + 1] << 8) | s[i]) - esi;

                edx = (edx + ebx) ^ (esi >> 28) ^ (esi << 4);
                esi += edi;
                edi = (edi - edx) ^ (edx >> 26) ^ (edx << 6);
                edx += esi;
                esi = (esi - edi) ^ (edi >> 24) ^ (edi << 8);
                edi += edx;
                ebx = (edx - esi) ^ (esi >> 16) ^ (esi << 16);
                esi += edi;
                edi = (edi - ebx) ^ (ebx >> 13) ^ (ebx << 19);
                ebx += esi;
                esi = (esi - edi) ^ (edi >> 28) ^ (edi << 4);
                edi += ebx;
            }

            if (s.Length - i > 0)
            {
                switch (s.Length - i)
                {
                    case 12:
                        esi += (uint)s[i + 11] << 24;
                        goto case 11;
                    case 11:
                        esi += (uint)s[i + 10] << 16;
                        goto case 10;
                    case 10:
                        esi += (uint)s[i + 9] << 8;
                        goto case 9;
                    case 9:
                        esi += (uint)s[i + 8];
                        goto case 8;
                    case 8:
                        edi += (uint)s[i + 7] << 24;
                        goto case 7;
                    case 7:
                        edi += (uint)s[i + 6] << 16;
                        goto case 6;
                    case 6:
                        edi += (uint)s[i + 5] << 8;
                        goto case 5;
                    case 5:
                        edi += (uint)s[i + 4];
                        goto case 4;
                    case 4:
                        ebx += (uint)s[i + 3] << 24;
                        goto case 3;
                    case 3:
                        ebx += (uint)s[i + 2] << 16;
                        goto case 2;
                    case 2:
                        ebx += (uint)s[i + 1] << 8;
                        goto case 1;
                    case 1:
                        ebx += (uint)s[i];
                        break;
                }

                esi = (esi ^ edi) - ((edi >> 18) ^ (edi << 14));
                ecx = (esi ^ ebx) - ((esi >> 21) ^ (esi << 11));
                edi = (edi ^ ecx) - ((ecx >> 7) ^ (ecx << 25));
                esi = (esi ^ edi) - ((edi >> 16) ^ (edi << 16));
                edx = (esi ^ ecx) - ((esi >> 28) ^ (esi << 4));
                edi = (edi ^ edx) - ((edx >> 18) ^ (edx << 14));
                eax = (esi ^ edi) - ((edi >> 8) ^ (edi << 24));

                return ((ulong)edi << 32) | eax;
            }

            return ((ulong)esi << 32) | eax;
        }
    }

   
}