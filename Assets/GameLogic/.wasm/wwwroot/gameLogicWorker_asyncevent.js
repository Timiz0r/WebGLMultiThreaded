import { dotnet } from './_framework/dotnet.js'

// https://github.com/ilonatommy/reactWithDotnetOnWebWorker/blob/master/react/src/client.js

let assemblyExports = null;
let startupError = undefined;

try {
    const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

    setModuleImports("AsyncEventExample", {
        event: (name, data) => sendEvent(name, data)
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
                const time = Number(e.data.time);
                assemblyExports.AsyncEventExample.Update(time);
            default:
                throw new Error("Unknown command: " + e.data.command);
        }
    }
    catch (err) {
        sendError(err)
    }
};

function sendEvent(name, data) {
    postMessage({ name, data });
}
function sendError(err) {
    sendEvent("error", err);
}