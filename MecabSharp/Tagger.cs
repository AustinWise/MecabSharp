using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Austin.MecabSharp
{
    public class Tagger : IDisposable
    {
        readonly Encoding mEnc = Encoding.UTF8;
        Interop.Mecab.MecabHandle mHand;

        public unsafe Tagger()
        {
            mHand = Interop.Mecab.CreateNew(0, new IntPtr(0));
            if (mHand.IsInvalid)
            {
                throw new Exception("Failed to create mecab.");
            }

            var dicInfo = Interop.Mecab.DictionaryInfo(mHand);
            switch (dicInfo->Charset)
            {
                case "UTF-8":
                    mEnc = Encoding.UTF8;
                    break;
                default:
                    throw new Exception("Unknown charset: " + dicInfo->Charset);
            }
        }

        public unsafe List<Node> Parse(string str)
        {
            var ret = new List<Node>();

            var strBytes = mEnc.GetBytes(str);

            if (strBytes.Length == 0)
                return ret;

            bool tookReference = false;
            try
            {
                //this add ref is to prevent the hand from being disposed out from under us
                mHand.DangerousAddRef(ref tookReference);

                fixed (byte* bytePtr = strBytes)
                {
                    //All use of the node pointer has to stay in this fixed statement.
                    //The node ptr refers back to the bytePtr.
                    var nodePtr = Interop.Mecab.SparseToNode(mHand, bytePtr, strBytes.Length);

                    while (nodePtr != null)
                    {
                        switch (nodePtr->stat)
                        {
                            case Interop.Mecab.NodeType.NOR:
                            case Interop.Mecab.NodeType.UNK:
                                {
                                    string surface = GetStringFromCharPointer(nodePtr->surface, nodePtr->length);
                                    string feature = GetStringFromCharPointer(nodePtr->feature);
                                    ret.Add(new Node(surface, feature));
                                }
                                break;
                            case Interop.Mecab.NodeType.BOS:
                                break;
                            case Interop.Mecab.NodeType.EOS:
                                break;
                            default:
                                throw new MecabException("Unsupported node type: " + nodePtr->stat);
                        }

                        nodePtr = nodePtr->next;
                    }
                }
            }
            finally
            {
                if (tookReference)
                    mHand.DangerousRelease();
            }

            return ret;
        }

        public unsafe string GetStringFromCharPointer(byte* bytes, int maxLength = int.MaxValue)
        {
            if (bytes == null)
                return string.Empty;
            int actualLength = 0;
            while (bytes[actualLength] != 0 && actualLength < maxLength)
            {
                actualLength++;
            }
            var buffer = new byte[actualLength];
            Marshal.Copy(new IntPtr(bytes), buffer, 0, actualLength);
            return mEnc.GetString(buffer);
        }

        public void Dispose()
        {
            this.mHand.Dispose();
        }
    }
}
