function Get-Hello {
    param ($name)
    Write-Output ""Hello, $name!""
}
Get-Hello -name 'World'