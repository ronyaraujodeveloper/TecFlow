$path = "C:\inetpub\tecflow\webui"
if (Test-Path $path) {
    $acl = Get-Acl $path

    # Permissão para o Grupo de Usuários do IIS
    $permission1 = "IIS_IUSRS","ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule1 = New-Object System.Security.AccessControl.FileSystemAccessRule($permission1)
    $acl.SetAccessRule($accessRule1)

    # Permissão para o Usuário de Pool Anônimo do IIS
    $permission2 = "IUSR","ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule($permission2)
    $acl.SetAccessRule($accessRule2)

    Set-Acl $path $acl
    Write-Host "Permissões de leitura e herança aplicadas com sucesso para $path"
}
