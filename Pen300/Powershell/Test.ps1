# Importa o módulo Active Directory
Import-Module ActiveDirectory

# Executa um comando que requer permissões de Domain Admin
Add-ADGroupMember -Identity "Domain Admins" -Members "L57653A"
