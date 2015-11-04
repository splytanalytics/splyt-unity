@ECHO off
@SET BASEDIR=%cd%
@ECHO Making splyt package...
@ECHO %BASEDIR%
@ECHO Builder.MakeSplytPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\SplytUnity" -executeMethod Builder.MakeSplytPackage
@ECHO Builder.MakeBPPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\SplytUnity" -executeMethod Builder.MakeBPPackage
@ECHO Done.
@PAUSE
