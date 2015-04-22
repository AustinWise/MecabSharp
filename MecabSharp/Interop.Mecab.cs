using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

static partial class Interop
{
    public static unsafe class Mecab
    {
        public const string DLL_NAME = "libmecab.dll";
        static Interop.Kernel32.DllDirectorySafeHandle sMecabDir;

        #region Structs
        public enum DictionaryType : int
        {
            /// <summary>
            /// This is a system dictionary.
            /// </summary>
            SYS_DIC = 0,

            /// <summary>
            /// This is a user dictionary.
            /// </summary>
            USR_DIC = 1,

            /// <summary>
            /// This is a unknown word dictionary.
            /// </summary>
            UNK_DIC = 2,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mecab_dictionary_info_t
        {
            public string FileName
            {
                //TODO: check these encodings
                get { return Marshal.PtrToStringAnsi(new IntPtr(filename)); }
            }

            public string Charset
            {
                get { return Marshal.PtrToStringAnsi(new IntPtr(charset)); }
            }

            /// <summary>filename of dictionary</summary>
            /// <remarks>On Windows, filename is stored in UTF-8 encoding</remarks>
            byte* filename;

            /// <summary>
            /// character set of the dictionary. e.g., "SHIFT-JIS", "UTF-8"
            /// </summary>
            byte* charset;

            /// <summary>
            /// How many words are registered in this dictionary.
            /// </summary>
            public uint size;

            /// <summary>
            /// dictionary type
            /// </summary>
            public DictionaryType type;

            /// <summary>
            /// left attributes size
            /// </summary>
            public uint lsize;

            /// <summary>
            /// right attributes size
            /// </summary>
            public uint rsize;

            /// <summary>
            /// version of this dictionary
            /// </summary>
            public ushort version;

            /// <summary>
            /// pointer to the next dictionary info.
            /// </summary>
            public mecab_dictionary_info_t* next;
        }

        public enum NodeType : byte
        {
            /// <summary>
            /// Normal node defined in the dictionary.
            /// </summary>
            NOR = 0,

            /// <summary>
            /// Unknown node not defined in the dictionary.
            /// </summary>
            UNK = 1,

            /// <summary>
            /// Virtual node representing a beginning of the sentence.
            /// </summary>
            BOS = 2,

            /// <summary>
            /// Virtual node representing a end of the sentence.
            /// </summary>
            EOS = 3,

            /// <summary>
            /// Virtual node representing a end of the N-best enumeration.
            /// </summary>
            EON = 4
        }

        /// <summary>
        /// Node structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mecab_node_t
        {
            /// <summary>
            /// pointer to the previous node.
            /// </summary>
            public mecab_node_t* prev;

            /// <summary>
            /// pointer to the next node.
            /// </summary>
            public mecab_node_t* next;

            /// <summary>
            /// pointer to the node which ends at the same position.
            /// </summary>
            public mecab_node_t* enext;

            /// <summary>
            /// pointer to the node which starts at the same position.
            /// </summary>
            public mecab_node_t* bnext;

            /// <summary>
            /// pointer to the right path.
            /// </summary>
            /// <remarks>
            /// this value is NULL if MECAB_ONE_BEST mode.
            /// </remarks>
            public mecab_path_t* rpath;

            /// <summary>
            /// pointer to the right path.
            /// </summary>
            /// <remarks>
            /// this value is NULL if MECAB_ONE_BEST mode.
            /// </remarks>
            public mecab_path_t* lpath;

            /// <summary>
            /// surface string
            /// </summary>
            /// <remarks>
            /// This value is not 0 terminated.
            /// You can get the length with length/rlength members.
            /// </remarks>
            public byte* surface;

            /// <summary>
            /// feature string
            /// </summary>
            public byte* feature;

            /// <summary>
            /// unique node id
            /// </summary>
            public uint id;

            /// <summary>
            /// length of the surface form.
            /// </summary>
            public ushort length;

            /// <summary>
            /// length of the surface form including white space before the morph.
            /// </summary>
            public ushort rlength;

            /// <summary>
            /// right attribute id
            /// </summary>
            public ushort rcAttr;

            /// <summary>
            /// left attribute id
            /// </summary>
            public ushort lcAttr;

            /// <summary>
            /// unique part of speech id. This value is defined in "pos.def" file.
            /// </summary>
            public ushort posid;

            /// <summary>
            /// character type
            /// </summary>
            public byte char_type;

            /// <summary>
            /// status of this model
            /// </summary>
            public NodeType stat;

            /// <summary>
            /// set 1 if this node is best node.
            /// </summary>
            public byte isbest;

            /// <summary>
            /// forward accumulative log summation.
            /// </summary>
            /// <remarks>
            /// This value is only available when MECAB_MARGINAL_PROB is passed.
            /// </remarks>
            public float alpha;

            /// <summary>
            /// backward accumulative log summation.
            /// </summary>
            /// <remarks>
            /// This value is only available when MECAB_MARGINAL_PROB is passed.
            /// </remarks>
            public float beta;

            /// <summary>
            /// marginal probability
            /// </summary>
            /// <remarks>
            /// This value is only available when MECAB_MARGINAL_PROB is passed.
            /// </remarks>
            public float prob;

            /// <summary>
            /// word cost.
            /// </summary>
            public short wcost;

            /// <summary>
            /// best accumulative cost from bos node to this node.
            /// </summary>
            public long cost;
        }

        /// <summary>
        /// Path structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct mecab_path_t
        {
            /// <summary>
            /// pointer to the right node
            /// </summary>
            public mecab_node_t* rnode;

            /// <summary>
            /// pointer to the next right path
            /// </summary>
            public mecab_path_t* rnext;

            /// <summary>
            /// pointer to the left node
            /// </summary>
            public mecab_node_t* lnode;

            /// <summary>
            /// pointer to the next left path
            /// </summary>
            public mecab_path_t* lnext;

            /// <summary>
            /// local cost
            /// </summary>
            public int cost;

            /// <summary>
            /// marginal probability
            /// </summary>
            public float prob;
        }
        #endregion

        #region Public Wrappers
        static void EnsureMecab()
        {
            lock (typeof(Mecab))
            {
                if (sMecabDir != null && !sMecabDir.IsInvalid)
                    return;

                var rcPath = (String)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\MeCab", "mecabrc", String.Empty);
                if (string.IsNullOrEmpty(rcPath))
                {
                    throw new Exception("Mecab not installed!");
                }
                string binPath, mecabDll;
                try
                {
                    var fi = new FileInfo(rcPath);
                    binPath = Path.Combine(fi.Directory.Parent.FullName, "bin");
                    mecabDll = Path.Combine(binPath, DLL_NAME);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not find " + DLL_NAME, ex);
                }
                if (!File.Exists(mecabDll))
                {
                    throw new Exception("Mecab DLL does not exist: " + mecabDll);
                }

                var hand = Kernel32.AddToSearchPath(binPath);

                //TODO: see if the version check can be loosened up.
                //If the function signatures or structures changes between versions we could crash.
                var versionStr = Marshal.PtrToStringAnsi(Mecab.mecab_version());
                if (versionStr != "0.996")
                {
                    throw new Exception("Invalid Mecab version!");
                }

                sMecabDir = hand;
            }
        }

        public static MecabHandle CreateNew(int argc, IntPtr argv)
        {
            EnsureMecab();
            return mecab_new(argc, argv);
        }

        public static mecab_dictionary_info_t* DictionaryInfo(MecabHandle mecab)
        {
            EnsureMecab();
            return mecab_dictionary_info(mecab);
        }

        public static mecab_node_t* SparseToNode(MecabHandle mecab, byte* str, int length)
        {
            EnsureMecab();
            var ret = mecab_sparse_tonode2(mecab, str, length);
            if (ret == null)
            {
                var msgPtr = mecab_strerror(mecab);
                string msg = "Unknown error.";
                if (msgPtr != null)
                {
                    msg = Marshal.PtrToStringAnsi(msgPtr);
                }
                throw new Austin.MecabSharp.MecabException(msg);
            }
            return ret;
        }
        #endregion

        #region PInvoke
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr mecab_version();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr mecab_strerror(MecabHandle mecab);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern MecabHandle mecab_new(int argc, IntPtr argv);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern mecab_node_t* mecab_sparse_tonode2(MecabHandle mecab, byte* str, int length);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern mecab_dictionary_info_t* mecab_dictionary_info(MecabHandle mecab);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void mecab_destroy(IntPtr mecab);
        #endregion


        public class MecabHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            protected MecabHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                Mecab.mecab_destroy(handle);
                return true;
            }
        }
    }
}