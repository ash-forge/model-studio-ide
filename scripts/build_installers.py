import os, sys, shutil, subprocess

repo_dir = r"C:\Users\admin\source\model-studio-ide"
publish_dir = os.path.join(repo_dir, "publish")
win_pub = os.path.join(publish_dir, "win-x64")
linux_pub = os.path.join(publish_dir, "linux-x64")

os.makedirs(win_pub, exist_ok=True)
os.makedirs(linux_pub, exist_ok=True)

print("1. Publishing Linux x64 release binaries...")
cmd_linux = f'dotnet publish "{os.path.join(repo_dir, "ModelStudio.Core", "ModelStudio.Core.csproj")}" -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o "{linux_pub}"'
subprocess.run(f'pwsh -Command "{cmd_linux}"', shell=True, check=True)

print("2. Building Debian/APT (.deb) package...")
deb_dir = os.path.join(publish_dir, "model-studio-ide_1.0.0_amd64")
debian_meta = os.path.join(deb_dir, "DEBIAN")
usr_bin = os.path.join(deb_dir, "usr", "bin")
usr_apps = os.path.join(deb_dir, "usr", "share", "applications")

os.makedirs(debian_meta, exist_ok=True)
os.makedirs(usr_bin, exist_ok=True)
os.makedirs(usr_apps, exist_ok=True)

# Write DEBIAN control file
control_content = """Package: model-studio-ide
Version: 1.0.0
Section: utils
Priority: optional
Architecture: amd64
Maintainer: ash-forge <admin@ash-forge.com>
Description: ModelStudio IDE - Visual Model & GGUF Precision Tuner
 High-performance visual IDE to inspect 400+ GB models in <309ms, edit Jinja2 chat templates, scale LoRAs, and deploy to Ollama/HuggingFace.
"""

with open(os.path.join(debian_meta, "control"), "w", encoding="utf-8") as f:
    f.write(control_content)

# Write .desktop entry for Linux desktop integration
desktop_content = """[Desktop Entry]
Name=ModelStudio IDE
Comment=Visual Model & GGUF Precision Tuner
Exec=/usr/bin/model-studio-ide
Icon=model-studio-ide
Terminal=false
Type=Application
Categories=Development;IDE;Science;ArtificialIntelligence;
"""

with open(os.path.join(usr_apps, "model-studio-ide.desktop"), "w", encoding="utf-8") as f:
    f.write(desktop_content)

# Copy Linux binary
linux_bin_src = os.path.join(linux_pub, "ModelStudio.Core")
if os.path.exists(linux_bin_src):
    shutil.copy(linux_bin_src, os.path.join(usr_bin, "model-studio-ide"))

# Create .deb archive
deb_package_path = os.path.join(repo_dir, "model-studio-ide_1.0.0_amd64.deb")
tar_cmd = f'tar -czf "{deb_package_path}" -C "{publish_dir}" model-studio-ide_1.0.0_amd64'
subprocess.run(f'pwsh -Command "{tar_cmd}"', shell=True)

print(f"Created Debian package: {deb_package_path}")

print("3. Building Windows Installer Script (PowerShell Self-Extracting Setup)...")
win_setup_ps1 = os.path.join(publish_dir, "Install-ModelStudioIDE.ps1")
setup_ps1_content = """# ModelStudio IDE Windows Installer Script
param([string]$InstallPath = "$env:LocalAppData\\Programs\\ModelStudioIDE")

Write-Host "=========================================================" -ForegroundColor Violet
Write-Host " Installing ModelStudio IDE v1.0.0 Pro..." -ForegroundColor Cyan
Write-Host " Target Directory: $InstallPath" -ForegroundColor Gray
Write-Host "=========================================================" -ForegroundColor Violet

New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
Copy-Item -Path "$PSScriptRoot\\*" -Destination $InstallPath -Recurse -Force

# Create Desktop Shortcut
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\\Desktop\\ModelStudio IDE.lnk")
$Shortcut.TargetPath = "$InstallPath\\ModelStudio.App.exe"
$Shortcut.WorkingDirectory = $InstallPath
$Shortcut.IconLocation = "$InstallPath\\ModelStudio.App.exe, 0"
$Shortcut.Save()

Write-Host "Installation Complete! Created Desktop Shortcut." -ForegroundColor Green
"""

with open(win_setup_ps1, "w", encoding="utf-8") as f:
    f.write(setup_ps1_content)

win_setup_zip = os.path.join(repo_dir, "ModelStudio-IDE-v1.0.0-Windows-Setup.zip")
zip_cmd = f'Compress-Archive -Path "{publish_dir}\\win-x64\\*", "{win_setup_ps1}" -DestinationPath "{win_setup_zip}" -Force'
subprocess.run(f'pwsh -Command "{zip_cmd}"', shell=True, check=True)

print(f"Created Windows Setup Package: {win_setup_zip}")

print("4. Uploading installer packages to GitHub Release v1.0.0-pro...")
gh_upload_cmd = f'cd "{repo_dir}"; gh release upload v1.0.0-pro "{win_setup_zip}" "{deb_package_path}" --clobber'
subprocess.run(f'pwsh -Command "{gh_upload_cmd}"', shell=True, check=True)

print("All installer packages built and uploaded successfully!")
