param(
    [switch]$IsLocalbuild = $false
)

if ($IsLocalbuild) {
    &  .\builds\build.ps1 -IsLocalbuild   
}else{
    &  .\builds\build.ps1 
}

