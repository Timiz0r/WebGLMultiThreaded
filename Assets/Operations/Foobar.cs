using System.Threading;

namespace WebGLMultiThreaded
{
    // since all of this gets built for a non-Unity project, nothing in this folder should have no dependencies on Unity.
    public static class Foobar
    {
        public static FoobarResult Execute(int num)
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
