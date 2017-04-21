#!groovy?


pipeline {
	agent any
	environment {
		// Begin of Config
		def gitURL = 'https://github.com/Metrilus/MetriCam2'
		def gitBranch = 'master'
		def filesContainingAssemblyVersion = 'SolutionAssemblyInfo.cs'
						
		def solution = '"MetriCam2 SDK.sln"'
		def msbuildToolName = 'MSBuild Release/x64 [v4.0.30319]'
		def msbuildArgs = '/p:Configuration=Release;Platform=x64'
		// For per-project encryption config see map at the beginning of this file.
		def dllsToDeploy = 'CookComputing.XmlRpcV2 MetriCam2.Cameras.CameraTemplate MetriCam2.Cameras.ifm MetriCam2.Cameras.SVS MetriCam2.Cameras.TheImagingSource MetriCam2.Cameras.UEye MetriCam2.Cameras.V3S MetriCam2.Cameras.WebCam MetriCam2 Metrilus.Util Newtonsoft.Json TIS.Imaging.ICImagingControl33'
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
				echo 'Run reference checker'
				bat '%REFERENCE_CHECKER% -j "%JOB_NAME%" "%WORKSPACE%"'
				echo 'Build'
				bat "${tool msbuildToolName} ${solution} ${msbuildArgs}"
			}
		}
		stage('Deploy') {
			environment {
				def VERSION = 'latest'
				def PUBLISH_DIR = "Z:\\\\releases\\\\MetriCam2\\\\git\\\\${VERSION}\\\\"
				def BIN_DIR = "${PUBLISH_DIR}bin\\\\"
				def RELEASE_DIR = 'bin\\\\x64\\\\Release\\\\'
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
				bat '''FOR %%p IN (%dllsToDeploy%) DO (
						COPY /Y "%RELEASE_DIR%%%p.dll" "%BIN_DIR%"
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