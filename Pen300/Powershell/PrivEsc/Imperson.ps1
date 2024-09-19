# Defina as credenciais hardcoded
$username = "devan\marcelo"
$password = "marcelo3"
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force
$credentials = New-Object System.Management.Automation.PSCredential($username, $securePassword)

# Crie uma sessão remota com as credenciais
$session = New-PSSession -ComputerName localhost -Credential $credentials

# Execute um comando remoto para listar o conteúdo do diretório
$directoryPath = "C:\Path\To\Directory"
Invoke-Command -Session $session -ScriptBlock {
    param ($path)
    Get-ChildItem -Path $path
} -ArgumentList $directoryPath

# Feche a sessão
Remove-PSSession -Session $session
