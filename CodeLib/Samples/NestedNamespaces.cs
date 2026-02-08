using System;
using System.Collections.Generic;
using System.Text;

namespace CodeLib.Samples
{
    namespace Foo
    {
        public class Mork
        {
            private bool Fork = true;
            public int Foo() => Fork ? 1 + 2 : "hello world".Length;
        }
    }

    namespace Bar.Baz
    {
        public enum Mork
        {
            Hello,
            World
        }
    }

    namespace Foo
    {
        public class Pickle
        {
            public int Bar() => 10 * 2;
        }
    }
}
