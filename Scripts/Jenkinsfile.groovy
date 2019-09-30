#!groovy

pipeline {
    agent any

    environment {
        def currentBranch = "${env.GITHUB_BRANCH_NAME}"
        def msbuildToolName = 'MSBuild Release/x64 [v15.0 / VS2017]'
        def solutionFilename = 'MetriCam2_SDK.sln'

        // All these variables depend upon the version number, as defined by GitVersion during the build.
        // The variables are defined here to make them globally available, and initialized in the Pre-Build stage.
        def nugetVersion = ""     // Includes Major.Minor.Patch, Build (or some other counter), and the branch name, e.g. 16.0.1-build-nuget-package.1
        def releaseVersion = ""   // Major.Minor.Patch.Build, e.g. 16.0.1.456
        def niceVersion = ""      // Major.Minor.Patch, e.g. 16.0.1
        def releaseFolder = ""    // Path where the release will be published, relative to RELEASE_PATH
        def releaseDirectory = "" // Absolute path where the release will be published

		def targetFrameworks = "net45 net472 netstandard2.0"
        def releaseLibraryDirectory = "lib"
        def folderSuffixDebug = "_debug"

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
                    @echo GitVersion
                    %GitVersion%
                    %GitVersion% /output buildserver
                    '''
                script {
                    def props = readProperties  file: 'gitversion.properties'
                    def vMajor = props['GitVersion_Major']
                    def vMinor = props['GitVersion_Minor']
                    def vPatch = props['GitVersion_Patch']
                    def currentBuildNumber = currentBuild.number.toString();
                    def releaseVersionInterfix = getReleaseVersionInterfix(currentBranch);
                    // Update global variables
                    releaseVersion = getFullReleaseVersion(currentBranch, vMajor, vMinor, vPatch);
                    niceVersion = getNiceReleaseVersion(vMajor, vMinor, vPatch);
                    nugetVersion = "${niceVersion}${releaseVersionInterfix}.${currentBuildNumber}"
                    releaseFolder = getReleaseFolder(currentBranch, niceVersion)
                    releaseDirectory = "Z:\\releases\\MetriCam2\\${releaseFolder}"
                    // Output which might be useful for debugging the build job
                    echo "vMajor.MINOR.BUILD = ${vMajor}.${vMinor}.${vPatch}"
                    echo "releaseVersion = ${releaseVersion}"
                    echo "niceVersion = ${niceVersion}"
                    echo "nugetVersion = ${nugetVersion}"
                    echo "releaseFolder = ${releaseFolder}"
                    echo "releaseDirectory = ${releaseDirectory}"
                }

                echo "Setting version number for C# projects..."
                bat "\"Scripts\\SetVersion.cmd\" \"Directory.Build.props\" ${releaseVersion} ${nugetVersion}"

                echo "Setting version for C++/CLI projects..."
                bat "\"Scripts\\Set Assembly-Info Version.cmd\" \"SolutionAssemblyInfo.h\" ${releaseVersion}"
            }
        }

        stage('Build') {
            steps {
                bat "\"${tool msbuildToolName}\" ${solutionFilename} /p:Configuration=Release;Platform=x64"
                bat "\"${tool msbuildToolName}\" ${solutionFilename} /p:Configuration=Debug;Platform=x64"
            }
        }

        stage('Publish') {
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
                    for %%t in (%targetFrameworks%) do (
                        mkdir \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}\"
                        if errorlevel 1 GOTO StepFailed
                        mkdir \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}${folderSuffixDebug}\"
                        if errorlevel 1 GOTO StepFailed
                    )
                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    """

                bat """
                    @echo Publishing Libraries and Dependencies ...

                    for %%t in (%targetFrameworks%) do (
                        xcopy /Y /V \"bin\\Release\\%%t\\*.dll\" \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}\"
                        if errorlevel 1 GOTO StepFailed
                        xcopy /Y /V \"bin\\Release\\%%t\\*.pdb\" \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}\"
                        if errorlevel 1 GOTO StepFailed
                        xcopy /Y /V \"bin\\Debug\\%%t\\*.dll\" \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}${folderSuffixDebug}\"
                        if errorlevel 1 GOTO StepFailed
                        xcopy /Y /V \"bin\\Debug\\%%t\\*.pdb\" \"${releaseDirectory}\\%%t\\${releaseLibraryDirectory}${folderSuffixDebug}\"
                        if errorlevel 1 GOTO StepFailed
                    )

                    @echo Publishing Camera-specific .props Files ...

                    copy /Y \"BetaCameras\\OrbbecOpenNI\\MetriCam2.Orbbec.props\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    copy /Y \"BetaCameras\\Kinect4Azure\\MetriCam2.Kinect4Azure.props\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    """

                bat """
                    @echo Publishing License Files ...

                    copy \"License.txt\" \"${releaseDirectory}\"
                    if errorlevel 1 GOTO StepFailed
                    exit /b 0

                    :StepFailed
                    echo The step failed
                    exit /b 1
                    """

                bat """
                    @echo Creating last_build_datetime.txt ...
                    echo %BUILD_DATETIME%> \"${releaseDirectory}\\last_build_datetime.txt\"

                    @echo Creating build_info.txt ...
                    echo Build Trigger   : CI> \"${releaseDirectory}\\build_info.txt\"
                    echo Release Version : ${releaseVersion} >> \"${releaseDirectory}\\build_info.txt\"
                    echo Nice Version    : ${niceVersion} >> \"${releaseDirectory}\\build_info.txt\"
                    echo Nuget Version   : ${nugetVersion} >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build Name      : %JOB_NAME% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build DateTime  : %BUILD_DATETIME% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build ID        : %BUILD_ID% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build NR        : %BUILD_NUMBER% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build Tag       : %BUILD_TAG% >> \"${releaseDirectory}\\build_info.txt\"
                    echo Build URL       : %BUILD_URL% >> \"${releaseDirectory}\\build_info.txt\"
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

def getReleaseVersionInterfix(String branchName) {
    return isStableBuild(branchName)
        ? ""
        : "-${branchName}";
}

def getFullReleaseVersion(String branchName, String major, String minor, String patch) {
    def releaseRevision = currentBuild.number.toString();
    return "${major}.${minor}.${patch}.${releaseRevision}";
}

def getNiceReleaseVersion(String major, String minor, String patch) {
    return "${major}.${minor}.${patch}";
}

def getReleaseFolder(String branchName, String niceVersion) {
    def currentBuildNumber = currentBuild.number.toString();
    if (isStableBuild(branchName)) { return "v.${niceVersion}"; }
    if (isMasterBuild(branchName)) { return ".unstable\\${branchName}\\${currentBuildNumber}"; }
    return ".unstable\\${branchName}";
}

def isStableBuild(String branchName) {
    return branchName == "stable";
}

def isMasterBuild(String branchName) {
    return branchName == "master";
}

// Steps which are not converted to pipeline, yet (or untested with p.)
// * Build docs
// * Patch doxyfile
