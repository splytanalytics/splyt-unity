@ECHO off
@SET BASEDIR=%cd%
@ECHO Making splyt package...
@ECHO %BASEDIR%
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\samples\BubblePop" -executeMethod Builder.MakeSplytPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\samples\BubblePop" -executeMethod Builder.MakeBPPackage
@ECHO Done.
@PAUSE
