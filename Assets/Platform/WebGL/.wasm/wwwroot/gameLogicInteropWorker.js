import { dotnet } from "./_framework/dotnet.js";
import * as Comlink from "https://unpkg.com/comlink/dist/esm/comlink.mjs";

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const assemblyExports = await getAssemblyExports(config.mainAssemblyName);
const interop = assemblyExports.GameLogicInterop;

setModuleImports("GameLogic", {
    StateChanged: data => { interop.subscriber && interop.subscriber(data); }
});

Comlink.expose(interop);
postMessage("_init");
