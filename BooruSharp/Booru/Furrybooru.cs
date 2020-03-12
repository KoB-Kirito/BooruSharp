﻿using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace BooruSharp.Booru
{
    public sealed class Furrybooru : Template.Gelbooru02
    {
        public Furrybooru(BooruAuth auth = null) : base("furry.booru.org", auth)
        { }

        public override bool IsSafe()
            => false;
    }
}
