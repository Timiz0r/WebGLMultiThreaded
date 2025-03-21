using System.Threading;

namespace WebGLMultiThreaded
{
    public static class OperationRunner
    {
        public static FoobarResult Foobar(int num)
        {
            //some expensive operation
            Thread.Sleep(500);

            return new()
            {
                Foo = num + 1337,
                Bar = $"I like {num}!"
            };
        }

        // FUTURE: an async-await example would be handy
        // FUTURE: a web worker pool would also be interesting. though, I suspect an oss solution already exists.
        //    implementing sounds fun, though!
    }

    public class FoobarResult
    {
        public int Foo;
        public string Bar;
    }
}
