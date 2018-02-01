#!groovy?

pipeline {
	agent any
	environment {
		// Begin of Config
		def gitURL = 'https://github.com/Metrilus/MetriCam2'
		def gitBranch = 'master'
		def filesContainingAssemblyVersion = 'SolutionAssemblyInfo.cs'
						
		def solution = 'MetriCam2_SDK.sln'
		def msbuildToolName = 'MSBuild Release/x64 [v15.0 / VS2017]'
		def msbuildArgs = '/p:Configuration=Release;Platform=x64'
		def dllsToDeployX64 = 'CookComputing.XmlRpcV2 MetriCam2.Cameras.ifm MetriCam2.Cameras.Kinect2 MetriCam2.Cameras.OrbbecOpenNI MetriCam2.Cameras.RealSense2 MetriCam2.Cameras.Sick.TiM561 MetriCam2.Cameras.Sick.VisionaryT MetriCam2.Cameras.SVS MetriCam2.Cameras.UEye MetriCam2.Cameras.WebCam'
		def dllsToDeployAnyCPU = 'MetriCam2.Controls MetriCam2 Metrilus.Util Newtonsoft.Json'
		// End of Config
		def BUILD_DATETIME = new Date(currentBuild.startTimeInMillis).format("yyyyMMdd-HHmm")
	}
	stages {
		stage('Checkout') {
			steps {
				retry(3) {
					deleteDir()
				}
				git branch: gitBranch, url: gitURL
			}
		}
		stage('Prebuild') {
			steps {
				echo "Inject version number: ${BUILD_DATETIME}"
				bat '''
					FOR %%f IN (%filesContainingIsInternal%) DO (
						ATTRIB -R %%f
						@powershell -Command "(get-content %%f) |foreach-object {$_ -replace \\"NOT_A_RELEASE_BUILD\\", \\"%BUILD_DATETIME%\\"} | set-content %%f"
					)
					'''
				echo "Update AssemblyVersion for generated assembly files"
				bat '''
					FOR %%f IN (%filesContainingAssemblyVersion%) DO (
						ATTRIB -R %%f
						@powershell -Command "(get-content %%f) |foreach-object {$_ -replace \\"AssemblyVersion^(^\\^(\\"\\"\\d+\\.\\d+\\.\\d+^)\\.\\d+^(.+^)\\", 'AssemblyInformationalVersion$1.%BUILD_DATETIME%$2'} | set-content %%f"
					)
					'''
			}
		}
		stage('Build') {
			steps {
				echo 'Restore NuGet packages'
				bat '%NUGET_EXE% restore'
				echo 'Build'
				bat "\"${tool msbuildToolName}\" ${solution} ${msbuildArgs}"
			}
		}
		stage('Deploy') {
			environment {
				def VERSION = 'latest'
				def PUBLISH_DIR = "Z:\\\\releases\\\\MetriCam2\\\\git\\\\${VERSION}\\\\"
				def BIN_DIR = "${PUBLISH_DIR}lib\\\\"
				def RELEASE_DIR_X64 = 'bin\\\\x64\\\\Release\\\\'
				def RELEASE_DIR_ANYCPU = 'bin\\\\Release\\\\'
			}
			steps {
				echo 'Publish artefacts to Z:\\releases\\'
				
				echo 'Prepare Folders'
				retry(3) {
					bat '''
						IF EXIST "%PUBLISH_DIR%" RMDIR /S /Q "%PUBLISH_DIR%"
						MKDIR "%PUBLISH_DIR%"
						IF EXIST "%BIN_DIR%" RMDIR /S /Q "%BIN_DIR%"
						MKDIR "%BIN_DIR%"
						'''
				}
				echo 'Publish dependencies to Release Folder'
				bat '''
					FOR %%p IN (%dllsToDeployX64%) DO (
						COPY /Y "%RELEASE_DIR_X64%%%p.dll" "%BIN_DIR%"
					)
					FOR %%p IN (%dllsToDeployAnyCPU%) DO (
						COPY /Y "%RELEASE_DIR_ANYCPU%%%p.dll" "%BIN_DIR%"
					)
					'''
				echo 'Write Build Timestamp to file'
				bat 'ECHO %BUILD_DATETIME% > "%PUBLISH_DIR%last_build_datetime.txt"'
				echo 'Write to Info File'
				bat '''
					ECHO Build Name     : %JOB_NAME% > "%PUBLISH_DIR%build_info.txt"
					ECHO Build DateTime : %BUILD_DATETIME% >> "%PUBLISH_DIR%build_info.txt"
					ECHO Build ID       : %BUILD_ID% >> "%PUBLISH_DIR%build_info.txt"
					ECHO Build NR       : %BUILD_NUMBER% >> "%PUBLISH_DIR%build_info.txt"
					ECHO Build Tag      : %BUILD_TAG% >> "%PUBLISH_DIR%build_info.txt"
					ECHO Build URL      : %BUILD_URL% >> "%PUBLISH_DIR%build_info.txt"
					'''
			}
		}
	}
}
// Steps which are not converted to pipeline, yet (or untested with p.)
// * Inject BUILD_DATETIME into AssemblyInfo files
// * Modified version numbers for nightly/rolling builds
// * Build docs
// * Patch doxyfile
// * Run tests
// * Encrypt
// * Deploy
// * Activate Chuck Norris
// * Create a tag in git?
// * E-mail Notification
