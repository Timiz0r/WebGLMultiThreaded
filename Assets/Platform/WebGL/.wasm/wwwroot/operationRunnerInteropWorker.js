import { dotnet } from "./_framework/dotnet.js";
import * as Comlink from "https://unpkg.com/comlink/dist/esm/comlink.mjs";

const { getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const assemblyExports = await getAssemblyExports(config.mainAssemblyName);
const interop = assemblyExports.OperationInterop;

Comlink.expose(interop);