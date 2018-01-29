$script_path = $myinvocation.mycommand.path
$script_folder = Split-Path $script_path -Parent
$aspnetcoredll = Join-Path $script_folder '..\src\AspNetCore\bin\Debug\x64\aspnetcore.dll'
$aspnetcorerhdll = Join-Path $script_folder '..\src\RequestHandler\bin\Debug\x64\aspnetcorerh.dll'
echo $aspnetcoredll
echo $aspnetcorerhdll
Copy-Item -Force -Path  $aspnetcoredll -Destination 'C:\Windows\System32\inetsrv\aspnetcore.dll' 
Copy-Item -Force -Path $aspnetcorerhdll -Destination 'C:\Windows\System32\inetsrv\aspnetcorerh.dll' 