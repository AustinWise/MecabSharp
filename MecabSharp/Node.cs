using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Austin.MecabSharp
{
    public class Node
    {
        static readonly char[] sSplitter = new char[] { ',' };

        public Node(string text, string feature)
        {
            this.Text = text;
            this.Feature = feature;

            //TODO: make sure this format for feature is always the same
            //品詞,品詞細分類1,品詞細分類2,品詞細分類3,活用形,活用型,原形,読み,発音

            var splits = feature.Split(sSplitter);
            if (splits.Length != 9)
                return; //TODO: unknown nodes have 7 entries

            PartOfSpeech = getFeature(splits, 0);
            PosSub1 = getFeature(splits, 1);
            PosSub2 = getFeature(splits, 2);
            PosSub3 = getFeature(splits, 3);
            ConjugatedForm = getFeature(splits, 4);
            InflectionType = getFeature(splits, 5);
            BaseForm = getFeature(splits, 6);
            Reading = getFeature(splits, 7);
            Pronunciation = getFeature(splits, 8);
        }

        static string getFeature(string[] splits, int ndx)
        {
            var ret = splits[ndx];
            if (ret == "*")
                ret = string.Empty;
            return ret;
        }

        public string Text { get; private set; }
        public string Feature { get; private set; }

        public string PartOfSpeech { get; private set; }
        public string PosSub1 { get; private set; }
        public string PosSub2 { get; private set; }
        public string PosSub3 { get; private set; }
        public string ConjugatedForm { get; private set; }
        public string InflectionType { get; private set; }
        public string BaseForm { get; private set; }
        public string Reading { get; private set; }
        public string Pronunciation { get; private set; }
    }
}
