import * as child_process from "child_process";
import * as commonLib from "metame-common";
import * as path from "path";
import { getDistDirPath } from "./path";
import * as dotenv from "dotenv";

dotenv.config();

async function main() {
  var version = require("./package.json").version;
  //copy binaries in compiled into bin
  const targetDir = getDistDirPath();
  const destDir = `${__dirname}\\metame-service-win-release\\bin`;

  // let fileNames: Array<string> = ["MetaMe.WindowsClient.exe", "MetaMe.Sensors.dll", "MetaMe.Core.dll"];

  // //now sign the files
  // var signToolExe = commonLib.getSignToolPath();
  // let signPaths: Array<string> = fileNames.map(fileName => path.join(targetDir, fileName));
  // for (let path of signPaths) {
  //   console.log(`Signing ${path}...`);
  //   // var signEngineCmd = `"${signToolExe}" sign /tr http://timestamp.comodoca.com /td sha256 /fd sha256 /a /v "${path}"`;
  //   var signEngineCmd = `"${signToolExe}" sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a /v "${path}"`;
  //   await commonLib.executeCommand(signEngineCmd);
  // }

  console.log(`Publishing version ${version}`);

  //clear repo
  let clonedPath: string = `${__dirname}\\metame-service-win-release`;
  commonLib.deleteDirectoryRecursive(clonedPath);

  //git clone
  let publishRepositoryUrl: string = process.env.PUBLISH_REPO;
  console.log(`Using repository: ${publishRepositoryUrl}`);
  var gitCloneCmd = `git clone ${publishRepositoryUrl}`;

  child_process.execSync(gitCloneCmd, { stdio: [0, 1, 2] });
  //set git name and email

  //check if tag already exists. npm version prepends a v at the beginning of the version
  var result = await commonLib.executeCommand(
    `(cd metame-service-win-release && git tag -l "v${version}")`
  );
  let matchingTag: string = result.stdout;

  if (matchingTag) {
    console.log(`${version} already exists`);
    console.log(`Publish stopped...`);
    return;
  }

  //clear destination
  commonLib.deleteDirectoryRecursive(destDir);

  var xcopyCmd = `xcopy ${targetDir} ${destDir}\\* /s /e /y`;
  child_process.execSync(xcopyCmd, { stdio: [0, 1, 2] });

  //add files, then commit
  console.log("Publishing to repository..");
  child_process.execSync(
    `(cd metame-service-win-release && git add . && git commit --allow-empty -m "Automated commit for ${version}")`,
    { stdio: [0, 1, 2] }
  );

  var remoteBranch = process.env.PUBLISH_REPO_BRANCH;

  //set the version and tag via npm version then commit both to devel and the tags
  child_process.execSync(
    `(cd metame-service-win-release && npm version ${version} && git push origin ${remoteBranch} && git push --tags origin ${remoteBranch})`,
    { stdio: [0, 1, 2] }
  );
}

main();
