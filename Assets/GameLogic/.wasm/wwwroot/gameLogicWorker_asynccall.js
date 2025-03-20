import { dotnet } from './_framework/dotnet.js'

// https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/react/src/client.js

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