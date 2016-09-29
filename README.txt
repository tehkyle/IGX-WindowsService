*==================================================*
|												   |
|        Ingeniux Windows Service Sample           |
|       Authors: Kyle Levien & Adam Busbin		   |
|   For use at the Ingeniux User Conference 2016.  |
|												   |
*==================================================*

Requirements:
- Ingeniux CMS Instance
- Visual Studio 2015
- Visual Studio Installer Extension
- SQL Database (optional: used for service updates)

Instructions:
- Copy Ingeniux CMS dlls from site instance into IGXActions/igxdlls (See: IGXActions/igxdlls/README.txt)
- Build IGX-WindowsService.sln
- Configure and run test project
- Run installer setup.exe to install as windows service.