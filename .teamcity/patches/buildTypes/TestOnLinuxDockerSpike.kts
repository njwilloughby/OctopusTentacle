package patches.buildTypes

import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.BuildType
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.dockerSupport
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.ScriptBuildStep
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.script
import jetbrains.buildServer.configs.kotlin.v2019_2.failureConditions.BuildFailureOnMetric
import jetbrains.buildServer.configs.kotlin.v2019_2.failureConditions.failOnMetricChange
import jetbrains.buildServer.configs.kotlin.v2019_2.ui.*

/*
This patch script was generated by TeamCity on settings change in UI.
To apply the patch, create a buildType with id = 'TestOnLinuxDockerSpike'
in the root project, and delete the patch script.
*/
create(DslContext.projectId, BuildType({
    id("TestOnLinuxDockerSpike")
    name = "Test on Linux (Docker Spike)"

    buildNumberPattern = "%dep.OctopusDeploy_OctopusShared_Build.build.number%"

    vcs {
        root(DslContext.settingsRoot)

        checkoutMode = CheckoutMode.ON_AGENT
    }

    steps {
        script {
            name = "dotnet vstest"
            scriptContent = """
                #!/bin/bash
                set -eux
                
                #dotnet vstest build/artifacts/linux-x64/Octopus.Shared.Tests.dll /logger:logger://teamcity /TestAdapterPath:/opt/TeamCity/BuildAgent/plugins/dotnet/tools/vstest15 /logger:console;verbosity=detailed
                dotnet vstest build/artifacts/linux-x64/Octopus.Shared.Tests.dll /logger:logger://teamcity /logger:console;verbosity=detailed
            """.trimIndent()
            dockerImagePlatform = ScriptBuildStep.ImagePlatform.Linux
            dockerPull = true
            dockerImage = "docker.packages.octopushq.com/octopusdeploy/tool-containers/test-ubuntu18"
            param("org.jfrog.artifactory.selectedDeployableServer.downloadSpecSource", "Job configuration")
            param("org.jfrog.artifactory.selectedDeployableServer.useSpecs", "false")
            param("org.jfrog.artifactory.selectedDeployableServer.uploadSpecSource", "Job configuration")
        }
    }

    failureConditions {
        failOnMetricChange {
            metric = BuildFailureOnMetric.MetricType.TEST_COUNT
            units = BuildFailureOnMetric.MetricUnit.DEFAULT_UNIT
            comparison = BuildFailureOnMetric.MetricComparison.LESS
            compareTo = value()
        }
    }

    features {
        commitStatusPublisher {
            publisher = github {
                githubUrl = "https://api.github.com"
                authType = personalToken {
                    token = "credentialsJSON:70b760a0-25e3-406b-9ed2-d73026115dc1"
                }
            }
        }
        dockerSupport {
            loginToRegistry = on {
                dockerRegistryId = "PROJECT_EXT_53"
            }
        }
    }

    dependencies {
        dependency(RelativeId("Build")) {
            snapshot {
                onDependencyFailure = FailureAction.CANCEL
                onDependencyCancel = FailureAction.CANCEL
            }

            artifacts {
                cleanDestination = true
                artifactRules = "publish/linux-x64=>build/artifacts/linux-x64"
            }
        }
    }

    requirements {
        exists("system.Octopus.Docker", "RQ_1")
        equals("system.Octopus.OSPlatform", "Linux", "RQ_2")
        equals("system.Octopus.Purpose", "Test", "RQ_3")
    }
    
    disableSettings("RQ_1", "RQ_2", "RQ_3")
}))

