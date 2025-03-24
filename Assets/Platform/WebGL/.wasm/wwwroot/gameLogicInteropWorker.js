import { dotnet } from './_framework/dotnet.js'

// largely based off of...
// https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/react/src/client.js

let assemblyExports = null;
let startupError = undefined;

try {
    const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

    setModuleImports("GameLogic", {
        StateChanged: data => sendEvent(data)
    });

    const config = getConfig();
    assemblyExports = await getAssemblyExports(config.mainAssemblyName);
}
catch (err) {
    startupError = err.message;
}

onmessage = e => {
    try {
        if (!assemblyExports) {
            throw new Error(startupError || "worker exports not loaded");
        }

        switch (e.data.command) {
            case "update":
                const time = e.data.time;
                // since we run this synchronously, this worker effectively queues up messages
                // if Update runs for a long time, this could *theoretically* back up the queue.
                // however, because of GameLogic.TimePerTick, we expect the queue to drain during these intervals.
                // FUTURE: still, implementing some "lag tracking" might be prudent.
                assemblyExports.GameLogicInterop.Update(time);
                break;
            default:
                throw new Error("Unknown command: " + e.data.command);
        }
    }
    catch (err) {
        sendError(err)
    }
};

function sendEvent(data) {
    postMessage({ command: "stateChanged", data });
}
function sendError(err) {
    postMessage({ command: "error", error: err.message });
}