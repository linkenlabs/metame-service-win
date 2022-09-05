import * as child_process from "child_process";
import { getMsBuildPath, deleteDirectoryRecursive } from "metame-common";
import { getDistDirPath } from "./path";
import * as fs from "fs-extra";

async function main() {
  const msBuildPath: string = await getMsBuildPath();
  const distPath: string = getDistDirPath();

  console.log("Cleaning existing artifacts...");
  deleteDirectoryRecursive(distPath);

  console.log("Building release version...");
  let solutionPath: string = `${__dirname}\\metame-service-win.sln`;
  let cmd: string = `"${msBuildPath}" ${solutionPath} /p:Configuration=Release /t:Rebuild`;
  child_process.execSync(cmd, { stdio: [0, 1, 2] });

  console.log("Copying to dist...");
  copyToDist();
}

main();

async function copyToDist() {
  const distPath: string = getDistDirPath();
  console.log("Ensuring dist...");
  await fs.ensureDir(distPath);
  console.log("Clearing dist...");
  await fs.emptyDir(distPath);

  console.log("Copy release folder to dist...");
  //copy release folder
  const targetDir: string = `${__dirname}\\metame-service\\bin\\Release`;
  await fs.copy(targetDir, distPath);
}
