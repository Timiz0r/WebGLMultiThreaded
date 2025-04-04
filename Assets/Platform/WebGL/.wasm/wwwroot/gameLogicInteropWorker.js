import { dotnet } from "./_framework/dotnet.js";
import * as Comlink from "https://unpkg.com/comlink/dist/esm/comlink.mjs";

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const assemblyExports = await getAssemblyExports(config.mainAssemblyName);

const subscriberHandler = new class {
    set(obj, prop, value) {
        if (prop === "subscriber") {
            this.subscriber = value;
        } else {
            obj[prop] = value;
        }
        return true;
    }
    get(obj, prop) {
        if (prop === "subscriber") {
            return this.subscriber;
        }
        return obj[prop];
    }
}();
const interop = new Proxy(assemblyExports.GameLogicInterop, subscriberHandler);

setModuleImports("GameLogic", {
    StateChanged: data => { interop.subscriber && interop.subscriber(data); }
});

Comlink.expose(interop);
postMessage("_init");
