#!groovy?

pipeline {
    agent any
    environment {
        def dllsToDeployX64 = 'CookComputing.XmlRpcV2 MetriCam2.Cameras.ifm MetriCam2.Cameras.Kinect2 MetriCam2.Cameras.OrbbecOpenNI MetriCam2.Cameras.Sick.TiM561 MetriCam2.Cameras.Sick.VisionaryT MetriCam2.Cameras.SVS MetriCam2.Cameras.UEye MetriCam2.Cameras.WebCam MetriCam2.Cameras.BaslerToF MetriCam2.Cameras.MatrixVision'
        def dllsToDeployAnyCPU = 'MetriCam2.Controls MetriCam2 Metrilus.Util Newtonsoft.Json MetriCam2.Cameras.RealSense2'
        def dllsToDeployNetStandard = 'MetriCam2.NetStandard Metrilus.Util.NetStandard MetriCam2.Cameras.RealSense2.NetStandard'

        def currentBranch = "${env.GITHUB_BRANCH_NAME}"
        def msbuildToolName = 'MSBuild Release/x64 [v15.0 / VS2017]'
        def solutionFilename = 'MetriCam2_SDK.sln'

        def releaseVersion = getReleaseVersion(currentBranch);
        def releaseFolder = getReleaseFolder(currentBranch, releaseVersion)

        def releaseDirectory = "Z:\\releases\\MetriCam2\\${releaseFolder}"
        def releaseLibraryDirectory = "${releaseDirectory}\\lib"
        def releaseSuffixNetStandard20 = "_netstandard2.0"
        def releaseSuffixDebug = "_debug"

        def STATUS_CONTEXT = 'MetriCam2 CI'
        def BUILD_DATETIME = new Date(currentBuild.startTimeInMillis).format("yyyyMMdd-HHmm")
        def BUILD_URL = "${BUILD_URL}".replace("http://", "https://").replace("-server.metrilus.informatik.uni-erlangen.de:8080", ".metrilus.de")
    }
    stages {
        stage('Pre-Build') {
            steps {
                echo "Set build status: pending"
                setBuildStatus("Build started", "PENDING", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")

                bat '''
                    @echo Restoring NuGet Packages ...
                    %NUGET_EXE% restore
                    '''

                bat '''
                    @echo Checking References ...
                    %REFERENCE_CHECKER% -j "%JOB_NAME%" "%WORKSPACE%"
                    '''

                bat """
                    @echo Updating Assembly-Info ...
                    \"Scripts\\Set Assembly-Info Version.cmd\" \"SolutionAssemblyInfo.cs\" ${releaseVersion}
                    """
            }
        }

        stage('Build') {
            steps {
                bat "\"${tool msbuildToolName}\" ${solutionFilename} /p:Configuration=Release;Platform=x64"
                bat "\"${tool msbuildToolName}\" ${solutionFilename} /p:Configuration=Debug;Platform=x64"
            }
        }

        stage('Publish') {
            environment {
                def RELEASE_DIR_X64 = 'bin\\x64\\Release\\'
                def DEBUG_DIR_X64 = 'bin\\x64\\Debug\\'
                def RELEASE_DIR_ANYCPU = 'bin\\Release\\'
                def DEBUG_DIR_ANYCPU = 'bin\\Debug\\'
                def RELEASE_DIR_NETSTANDARD = 'bin\\Release\\netstandard2.0\\'
                def DEBUG_DIR_NETSTANDARD = 'bin\\Debug\\netstandard2.0\\'
            }
            steps {
                script {
                    if (fileExists(releaseDirectory)) {
                        echo 'Deleting Release Directory ...'
                        dir(releaseDirectory) {
                            deleteDir()
                        }
                    }
                }

                bat """
                    echo Creating Release Directories ...

                    mkdir \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    mkdir \"${releaseLibraryDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    mkdir \"${releaseLibraryDirectory}${releaseSuffixDebug}\"
                    if errorlevel 1 GOTO StepFailed
                    mkdir \"${releaseLibraryDirectory}${releaseSuffixNetStandard20}\"
                    if errorlevel 1 GOTO StepFailed
                    mkdir \"${releaseLibraryDirectory}${releaseSuffixNetStandard20}${releaseSuffixDebug}\"
                    if errorlevel 1 GOTO StepFailed

                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    """

                bat '''
                    echo Publishing Libraries and Dependencies ...
                    REM No error check for PDB files, since dependencies don't have them.

                    FOR %%p IN (%dllsToDeployX64%) DO (
                        COPY /Y "%RELEASE_DIR_X64%%%p.dll" "%releaseLibraryDirectory%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%RELEASE_DIR_X64%%%p.pdb" "%releaseLibraryDirectory%"

                        COPY /Y "%DEBUG_DIR_X64%%%p.dll" "%releaseLibraryDirectory%%releaseSuffixDebug%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%DEBUG_DIR_X64%%%p.pdb" "%releaseLibraryDirectory%%releaseSuffixDebug%"
                    )
                    FOR %%p IN (%dllsToDeployAnyCPU%) DO (
                        COPY /Y "%RELEASE_DIR_ANYCPU%%%p.dll" "%releaseLibraryDirectory%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%RELEASE_DIR_ANYCPU%%%p.pdb" "%releaseLibraryDirectory%"

                        COPY /Y "%DEBUG_DIR_ANYCPU%%%p.dll" "%releaseLibraryDirectory%%releaseSuffixDebug%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%DEBUG_DIR_ANYCPU%%%p.pdb" "%releaseLibraryDirectory%%releaseSuffixDebug%"
                    )
                    FOR %%p IN (%dllsToDeployNetStandard%) DO (
                        COPY /Y "%RELEASE_DIR_NETSTANDARD%%%p.dll" "%releaseLibraryDirectory%%releaseSuffixNetStandard20%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%RELEASE_DIR_NETSTANDARD%%%p.pdb" "%releaseLibraryDirectory%%releaseSuffixNetStandard20%"

                        COPY /Y "%DEBUG_DIR_NETSTANDARD%%%p.dll" "%releaseLibraryDirectory%%releaseSuffixNetStandard20%%releaseSuffixDebug%"
                        if errorlevel 1 GOTO StepFailed
                        COPY /Y "%DEBUG_DIR_NETSTANDARD%%%p.pdb" "%releaseLibraryDirectory%%releaseSuffixNetStandard20%%releaseSuffixDebug%"
                    )

                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    '''

                bat """
                    echo Publishing License Files ...

                    copy \"License.txt\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    copy \"libraries\\LICENSE-MathNet\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    copy \"libraries\\NOTICE-log4net.md\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                
                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    """

                bat """
                    @echo Creating last_build_datetime.txt ...
                    echo %BUILD_DATETIME%> \"${releaseDirectory}\\last_build_datetime.txt\"
                    """

                bat """
                    @echo Creating build_info.txt ...
                    echo Build Trigger  : CI> \"${releaseDirectory}\\build_info.txt\"
                    echo Build Name     : %JOB_NAME% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build DateTime : %BUILD_DATETIME% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build ID       : %BUILD_ID% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build NR       : %BUILD_NUMBER% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build Tag      : %BUILD_TAG% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build URL      : %BUILD_URL% >> \"${releaseDirectory}\\build_info.txt\"
                    """
            }
        }

        stage('Tag') {
            when {
                expression {
                    return currentBranch == 'stable';
                }
            }
            steps {
                bat """
                    echo Tagging the Git Repository ...
                    \"Scripts\\Create Git Release Tag.bat\" v.${releaseVersion}
                    """
            }
        }
    }

    post {
        always { 
            step([$class: 'hudson.plugins.chucknorris.CordellWalkerRecorder'])
        }
        success {
            setBuildStatus("Build successful", "SUCCESS", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
        }
        unstable {
            setBuildStatus("Build successful", "SUCCESS", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
            step([$class: 'Mailer',
                notifyEveryUnstableBuild: true,
                sendToIndividuals: true])
        }
        failure {
            setBuildStatus("Build failed", "FAILURE", "${STATUS_CONTEXT}", "${GITHUB_BRANCH_HEAD_SHA}")
            step([$class: 'Mailer',
                notifyEveryUnstableBuild: true,
                sendToIndividuals: true])
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
        statusBackrefSource: [$class: "ManuallyEnteredBackrefSource", backref: "${BUILD_URL}"],
        statusResultSource: [$class: "ConditionalStatusResultSource", results: [[$class: "AnyBuildResult", message: message, state: state]] ]
    ]);
}

def getReleaseVersion(String branchName) {
    def releaseRevision = currentBuild.number.toString();
    return "stable" == branchName
        ? "${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_BUILD}.${releaseRevision}"
        : "0.0.0.${releaseRevision}";
}

def getReleaseFolder(String branchName, String releaseVersion) {
    def currentBuildNumber = currentBuild.number.toString();
    if ("stable" == branchName) { return "v.${releaseVersion}"; }
    if ("master" == branchName) { return ".unstable\\${branchName}\\${currentBuildNumber}"; }
    return ".unstable\\${branchName}";
}

// Steps which are not converted to pipeline, yet (or untested with p.)
// * Build docs
// * Patch doxyfile
