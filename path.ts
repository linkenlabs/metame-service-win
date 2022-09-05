import * as path from "path";

export function getDistDirPath() {
  var distPath = path.join(__dirname, "dist");
  return distPath;
}
