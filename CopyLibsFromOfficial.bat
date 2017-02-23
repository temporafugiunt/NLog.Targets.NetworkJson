@ECHO OFF
IF %1.==. GOTO ExecuteStatements
IF %1=="" GOTO ExecuteStatements
SET Repository_Location=%1
:ExecuteStatements
SET CurrentVersion=17.2


REM Get latest PV Logging App
.\.nuget\YeOldeGet .\_lib "PV Logging Service Layer" /VR:%CurrentVersion% /RECURSE /VERBOSE /BT:Debug


