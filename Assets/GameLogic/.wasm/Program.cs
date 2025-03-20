// wasmbrowser projects are intended to have a `public static void Main`
// we don't need it, so we just need some innocuous main (using top-level statements in this case )

// TODO: does this actually run? we don't actually call runMain, so kinda wouldn't expect it to.
System.Console.WriteLine("Loaded GameLogic wasm.");