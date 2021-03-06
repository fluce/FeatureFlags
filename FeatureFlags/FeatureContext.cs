﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography.X509Certificates;

namespace FeatureFlags
{
    public class FeatureContext
    {
        public DateTime DateTime { get; set; }
        public Guid? Uid { get; set; }
        public string Email { get; set; }

        internal InternalFeatureContext InternalFeatureContext { get; set; }
    }

    public class InternalFeatureContext
    {
        internal Stack<string> RecursiveStack { get; } = new Stack<string>();

        public IFeatures Features { get; set; }

    }
}
