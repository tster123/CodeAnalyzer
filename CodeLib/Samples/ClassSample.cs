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
            public int Foo() => 1 + 2;
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
