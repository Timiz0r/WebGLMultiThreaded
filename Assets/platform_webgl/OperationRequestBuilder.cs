
using System;
using System.Collections.Generic;
using AOT;
using UnityEngine;

// meant to hook together js-side calls of a certain convention.
// let's say we initiate a request with: int Request(int param, Action<int, string> success, Action<int, string> failure)
// where all of those ints (return value, callbacks) are request ids
// and the callbacks' strings take some result or error, serialized to a string, and deserializes it to whatever's appropriate
//
// those two callbacks above are provided by this implementation. the caller need not care about them.
//
// Create takes a couple callbacks for performing the deserialization of successes and failures.
// the returned value has a Launch that provides the internal success and failure callbacks.
// the caller of Launch hooks them up to the request method.
// the implementation here takes care of correlating request ids, invokes the deserialization callbacks, and returns the result
// all of this is done with Unity Awaitables for proper Unity compatibility.
public static class OperationRequestBuilder
{
    // instead of a two-step Create->Launch, we could just do it all in a Launch
    // opting for this to keep the callsite of Launch cleaner
    public static OperationRequestBuilder<TResult, TError> Create<TResult, TError>(
        Func<string, TResult> success, Func<string, TError> failure) => new(success, failure);
}

public record OperationResponse<TResult, TError>(
    bool IsSuccess,
    TResult Result,
    TError Error
);

public class OperationRequestBuilder<TResult, TError>
{
    // NOTE: since (afaik) Awaitables are meant to be invoked only off main thread, not handling thread safety
    private static Dictionary<int, RequestContext> requests = new();

    private readonly Func<string, TResult> successBuilder;
    private readonly Func<string, TError> failureBuilder;

    public OperationRequestBuilder(Func<string, TResult> successBuilder, Func<string, TError> failureBuilder)
    {
        this.successBuilder = successBuilder;
        this.failureBuilder = failureBuilder;
    }

    public Awaitable<OperationResponse<TResult, TError>> Launch(
        Func<Action<int, string>, Action<int, string>, Action<int>, int> requestLauncher)
    {
        AwaitableCompletionSource<OperationResponse<TResult, TError>> completionSource = new();

        int requestId = requestLauncher(Success, Failure, Initializing);
        if (requests.ContainsKey(requestId))
        {
            Debug.LogError($"Duplicate request id: {requestId}");
            // NOTE: kinda expect either result or error to be non-default/null
            // however, since it's implementation specific the format of error, we can't do any better
            // alternatives include throwing, warning and proceeding, or adding an additional delegate for failure to launch request
            completionSource.SetResult(new(IsSuccess: false, Result: default, Error: default));
            return completionSource.Awaitable;
        }

        requests.Add(requestId, new(completionSource, this));
        return completionSource.Awaitable;
    }

    [MonoPInvokeCallback(typeof(Action<int, string>))]
    private static void Success(int requestId, string result)
    {
        if (!RetrieveRequestContext(requestId, out RequestContext requestContext)) return;

        requestContext.CompletionSource.SetResult(
            new(
                IsSuccess: true,
                Result: requestContext.Builder.successBuilder(result),
                Error: default));
    }

    // quick note: c# 10 gets lambda attributes! unity on c# 9 though.
    // though, since pinvoke, afaik, requires static methods, perhaps we couldn't use lambdas anyway.
    // PS: do lambdas that ultimately don't capture anything get generated as static methods?
    // also, static lambda proposal exists
    [MonoPInvokeCallback(typeof(Action<int, string>))]
    private static void Failure(int requestId, string error)
    {
        if (!RetrieveRequestContext(requestId, out RequestContext requestContext)) return;

        requestContext.CompletionSource.SetResult(
            new(
                IsSuccess: false,
                Result: default,
                Error: requestContext.Builder.failureBuilder(error)));
    }

    [MonoPInvokeCallback(typeof(Action<int>))]
    private static void Initializing(int requestId)
    {
        if (!RetrieveRequestContext(requestId, out RequestContext requestContext)) return;

        // NOTE: not ideal for same reason as "duplicate request id" case described further up
        // it's an area that needs further design
        requestContext.CompletionSource.SetResult(
            new(
                IsSuccess: false,
                Result: default,
                Error: default));
    }

    private static bool RetrieveRequestContext(int requestId, out RequestContext requestContext)
    {
        if (!requests.TryGetValue(requestId, out requestContext))
        {
            Debug.LogError($"Unknown request id: {requestId}");
            return false;
        }
        requests.Remove(requestId);
        return true;
    }

    private record RequestContext(
        AwaitableCompletionSource<OperationResponse<TResult, TError>> CompletionSource,
        OperationRequestBuilder<TResult, TError> Builder);
}