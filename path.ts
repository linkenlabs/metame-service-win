import * as path from "path";
import * as fs from "fs";

export function getDotfuscatorCLIPath(): string {
  let dotfuscatorDir: string =
    `C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\IDE\\Extensions\\PreEmptiveSolutions\\DotfuscatorCE`;
  var cliPath = path.join(dotfuscatorDir, "dotfuscator.exe");
  if (!fs.existsSync(cliPath)) {
    return null;
  }
  return cliPath;
}

export function getDistDirPath() {
  var distPath = path.join(__dirname, "dist");
  return distPath;
}