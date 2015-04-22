using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Austin.MecabSharp
{
    public class Node
    {
        public Node(string text, string feature)
        {
            this.Text = text;
            this.Feature = feature;
        }

        public string Text { get; private set; }
        public string Feature { get; private set; }
    }
}
