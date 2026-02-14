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
}
