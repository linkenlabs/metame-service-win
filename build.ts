import * as child_process from "child_process";
import { getMsBuildPath, deleteDirectoryRecursive } from "metame-common";
import * as path from "path";
import { getDotfuscatorCLIPath, getDistDirPath } from "./path";
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

  console.log("Obfuscating...");
  runDotfuscator();

  console.log("Copying to dist...");
  copyToDist();
}

main();

function runDotfuscator() {
  //obfuscate code dlls
  //config moves from release folder > Dotfuscator folder
  const configPath: string = `${__dirname}\\Dotfuscator.xml`;
  var dotfuscatorPath = getDotfuscatorCLIPath();;
  if (!dotfuscatorPath) {
    throw 'dotfuscator.exe not found';
  }
  let cmd: string = `"${dotfuscatorPath}" ${configPath}`;
  child_process.execSync(cmd, { stdio: [0, 1, 2] });
}

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

  //copy over non .xml files
  console.log("Copy Dotfuscated folder to dist...");
  const dotfuscatorDir: string = path.join(__dirname, "Dotfuscated");
  var obfuscatedFiles: string[] = await fs.readdir(dotfuscatorDir);
  var relevantFiles = obfuscatedFiles.filter((fileName) => {
    return path.extname(fileName) != ".xml";
  });

  relevantFiles.forEach((fileName) => {
    var src = path.join(dotfuscatorDir, fileName);
    var dest = path.join(distPath, fileName);
    console.log(`Copying ${src} > ${dest}`);
    fs.copySync(src, dest);
  });

}

