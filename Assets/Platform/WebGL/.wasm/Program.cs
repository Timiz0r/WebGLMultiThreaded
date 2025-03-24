// wasmbrowser projects are intended to have a `public static void Main`
// we don't need it, so we just need some innocuous main (using top-level statements in this case)
// incidentally, since we don't use `dotnet.create()`'s `runMain`, this never actually gets run.
System.Console.WriteLine("Loaded GameLogic wasm.");