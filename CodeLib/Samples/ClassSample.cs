// foo
namespace CodeLib.Samples
{
    internal class ClassSample
    {

        public string Foo() => "Foo";

        public string Bar() => Foo() + "Bar";

        public bool Maybe { get; set; }
        public string Baz() => Maybe ? Foo() : Bar();

    }

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
