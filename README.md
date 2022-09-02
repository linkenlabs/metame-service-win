# metame-service-win

# Install
Node.js
Dotfuscator CLI
    - Install via Visual Studio Installer

# Release instructions
To build and push compiled artifacts to the metame-service-win-release npm package. Run the following
`npm run push`

The version in the package.json needs to not exist on the metame-service-win-release repository or the script will exitnode

# How to fix "Path does not exist: .... signtool.exe"
- Install Windows 10 SDK (10.0.17763.0)
   - todo: Make it work for all Windows 10 versions