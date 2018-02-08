#!groovy?

pipeline {
	agent any
	environment {
		// Begin of Config
		def filesContainingAssemblyVersion = 'SolutionAssemblyInfo.cs'
		def solution = 'MetriCam2_SDK.sln'
		def msbuildToolName = 'MSBuild Release/x64 [v15.0 / VS2017]'
		def msbuildArgsRelease = '/p:Configuration=Release;Platform=x64'
		def msbuildArgsDebug = '/p:Configuration=Debug;Platform=x64'
		def dllsToDeployX64 = 'CookComputing.XmlRpcV2 MetriCam2.Cameras.ifm MetriCam2.Cameras.Kinect2 MetriCam2.Cameras.OrbbecOpenNI MetriCam2.Cameras.Sick.TiM561 MetriCam2.Cameras.Sick.VisionaryT MetriCam2.Cameras.SVS MetriCam2.Cameras.UEye MetriCam2.Cameras.WebCam MetriCam2.Cameras.BaslerToF'
		def dllsToDeployAnyCPU = 'MetriCam2.Controls MetriCam2 Metrilus.Util Newtonsoft.Json MetriCam2.Cameras.RealSense2'
		def dllsToDeployNetStandard = 'MetriCam2.NetStandard Metrilus.Util.NetStandard MetriCam2.Cameras.RealSense2.NetStandard'
		// End of Config
		def STATUS_CONTEXT = 'MetriCam2 CI'
		def BUILD_DATETIME = new Date(currentBuild.startTimeInMillis).format("yyyyMMdd-HHmm")
		def DEBUG_SUFFIX = "_debug"
	}
	stages {
		stage('Prebuild') {
			steps {
				echo "Set build status pending"
				setBuildStatus("Build started", "PENDING", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
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
				bat "\"${tool msbuildToolName}\" ${solution} ${msbuildArgsRelease}"
				bat "\"${tool msbuildToolName}\" ${solution} ${msbuildArgsDebug}"
			}
		}
		stage('Deploy') {
			environment {
				def PUBLISH_DIR = "Z:\\\\releases\\\\MetriCam2\\\\.unstable\\\\${GITHUB_BRANCH_NAME}\\\\"
				def BIN_DIR = "${PUBLISH_DIR}lib\\\\"
				def BIN_DIR_DEBUG = "${PUBLISH_DIR}lib${DEBUG_SUFFIX}\\\\"
				def BIN_DIR_NETSTANDARD = "${PUBLISH_DIR}lib_netstandard2.0\\\\"
				def BIN_DIR_NETSTANDARD_DEBUG = "${PUBLISH_DIR}lib_netstandard2.0${DEBUG_SUFFIX}\\\\"
				def RELEASE_DIR_X64 = 'bin\\\\x64\\\\Release\\\\'
				def DEBUG_DIR_X64 = 'bin\\\\x64\\\\Debug\\\\'
				def RELEASE_DIR_ANYCPU = 'bin\\\\Release\\\\'
				def DEBUG_DIR_ANYCPU = 'bin\\\\Debug\\\\'
				def RELEASE_DIR_NETSTANDARD = 'bin\\\\Release\\\\netstandard2.0\\\\'
				def DEBUG_DIR_NETSTANDARD = 'bin\\\\Debug\\\\netstandard2.0\\\\'
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
						IF EXIST "%BIN_DIR_DEBUG%" RMDIR /S /Q "%BIN_DIR_DEBUG%"
						MKDIR "%BIN_DIR_DEBUG%"
						IF EXIST "%BIN_DIR_NETSTANDARD%" RMDIR /S /Q "%BIN_DIR_NETSTANDARD%"
						MKDIR "%BIN_DIR_NETSTANDARD%"
						IF EXIST "%BIN_DIR_NETSTANDARD_DEBUG%" RMDIR /S /Q "%BIN_DIR_NETSTANDARD_DEBUG%"
						MKDIR "%BIN_DIR_NETSTANDARD_DEBUG%"
						'''
				}
				echo 'Publish dependencies to Release Folder'
				bat '''
					FOR %%p IN (%dllsToDeployX64%) DO (
						COPY /Y "%RELEASE_DIR_X64%%%p.dll" "%BIN_DIR%"
					)
					FOR %%p IN (%dllsToDeployX64%) DO (
						COPY /Y "%DEBUG_DIR_X64%%%p.dll" "%BIN_DIR_DEBUG%"
						COPY /Y "%DEBUG_DIR_X64%%%p.pdb" "%BIN_DIR_DEBUG%"
					)
					FOR %%p IN (%dllsToDeployAnyCPU%) DO (
						COPY /Y "%RELEASE_DIR_ANYCPU%%%p.dll" "%BIN_DIR%"
					)
					FOR %%p IN (%dllsToDeployAnyCPU%) DO (
						COPY /Y "%DEBUG_DIR_ANYCPU%%%p.dll" "%BIN_DIR_DEBUG%"
						COPY /Y "%DEBUG_DIR_ANYCPU%%%p.pdb" "%BIN_DIR_DEBUG%"
					)
					FOR %%p IN (%dllsToDeployNetStandard%) DO (
						COPY /Y "%RELEASE_DIR_NETSTANDARD%%%p.dll" "%BIN_DIR_NETSTANDARD%"
					)
					FOR %%p IN (%dllsToDeployNetStandard%) DO (
						COPY /Y "%DEBUG_DIR_NETSTANDARD%%%p.dll" "%BIN_DIR_NETSTANDARD_DEBUG%"
						COPY /Y "%DEBUG_DIR_NETSTANDARD%%%p.pdb" "%BIN_DIR_NETSTANDARD_DEBUG%"
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
	post {
		success {
			setBuildStatus("Build successful", "SUCCESS", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
		}
		failure {
			setBuildStatus("Build failed", "FAILURE", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
		}
	}
}

def setBuildStatus(String message, String state, String context, String sha) {
    step([
        $class: "GitHubCommitStatusSetter",
        reposSource: [$class: "ManuallyEnteredRepositorySource", url: "https://github.com/Metrilus/MetriCam2"],
        contextSource: [$class: "ManuallyEnteredCommitContextSource", context: context],
        errorHandlers: [[$class: "ChangingBuildStatusErrorHandler", result: "UNSTABLE"]],
        commitShaSource: [$class: "ManuallyEnteredShaSource", sha: sha ],
        statusBackrefSource: [$class: "ManuallyEnteredBackrefSource", backref: "${BUILD_URL}flowGraphTable/"],
        statusResultSource: [$class: "ConditionalStatusResultSource", results: [[$class: "AnyBuildResult", message: message, state: state]] ]
    ]);
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
