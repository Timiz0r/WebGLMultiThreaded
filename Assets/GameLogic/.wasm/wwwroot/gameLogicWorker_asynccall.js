import { dotnet } from './_framework/dotnet.js'

// largely based off of...
// https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/react/src/client.js

// the "real" onmessage comes after all of the below dotnet-related awaits, so, until they're done, there is no onmessage
// in such a case, messages get dropped.
// it's not necessarily a big deal, but it leaves orphaned requests around.
//
// the easiest option is to temporarily respond with a message that allows those requests to be cleared.
// another hypothetical option would be to move dotnet initialization into onmessage. on the first message,
// we wait for initialization to finish, blocking that first message (and subsequent other).
// not sure how i'd implement it off the top of my head, though.
// dropping events is perhaps preferred, since it'll avoid the queue backing up.
onmessage = e => {
    postMessage({ requestId: e.data.requestId, command: "initializing" });
};

let assemblyExports = null;
let startupError = undefined;

try {
    const { getAssemblyExports, getConfig } = await dotnet.create();
    const config = getConfig();
    assemblyExports = await getAssemblyExports(config.mainAssemblyName);
}
catch (err) {
    startupError = err.message;
}

onmessage = e => {
    console.log("wat ", e.data.requestId);
    const baseResponse = { requestId: e.data.requestId };
    function sendResponse(result) {
        postMessage({ ...baseResponse, command: "response", result });
    }
    function sendError(err) {
        postMessage({ ...baseResponse, command: "error", error: err.message });
    }

    try {
        if (!assemblyExports) {
            throw new Error(startupError || "worker exports not loaded");
        }

        switch (e.data.command) {
            case "update":
                const time = e.data.time;
                const result = assemblyExports.AsyncCallExample.Update(time);
                return sendResponse(result)
            default:
                throw new Error("Unknown command: " + e.data.command);
        }
    }
    catch (err) {
        sendError(err)
    }
};