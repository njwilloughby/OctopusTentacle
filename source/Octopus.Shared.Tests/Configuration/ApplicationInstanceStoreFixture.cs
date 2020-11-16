﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Octopus.Diagnostics;
using Octopus.Shared.Configuration;
using Octopus.Shared.Configuration.Instances;
using Octopus.Shared.Util;

namespace Octopus.Shared.Tests.Configuration
{
    [TestFixture]
    public class ApplicationInstanceStoreFixture
    {
        IRegistryApplicationInstanceStore registryStore;
        IOctopusFileSystem fileSystem;
        ApplicationInstanceStore instanceStore;

        [SetUp]
        public void Setup()
        {
            registryStore = Substitute.For<IRegistryApplicationInstanceStore>();
            fileSystem = Substitute.For<IOctopusFileSystem>();
            var log = Substitute.For<ILog>();
            instanceStore = new ApplicationInstanceStore(new StartUpPersistedInstanceRequest(ApplicationName.OctopusServer, "instance 1"), log, fileSystem, registryStore);
        }

        [Test]
        public void ListInstance_NoRegistryEntries_ShouldListFromFileSystem()
        {
            registryStore.GetListFromRegistry().Returns(Enumerable.Empty<ApplicationInstanceRecord>());
            fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            fileSystem.EnumerateFiles(Arg.Any<string>()).Returns(new List<string> { "file1", "file2" });
            fileSystem.FileExists(Arg.Any<string>()).Returns(true);
            fileSystem.ReadFile(Arg.Is("file1")).Returns("{\"Name\": \"instance1\",\"ConfigurationFilePath\": \"configFilePath1\"}");
            fileSystem.ReadFile(Arg.Is("file2")).Returns("{\"Name\": \"instance2\",\"ConfigurationFilePath\": \"configFilePath2\"}");

            var instances = instanceStore.ListInstances();
            instances.Should()
                .BeEquivalentTo(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "configFilePath1"),
                    new ApplicationInstanceRecord("instance2", "configFilePath2")
                });
        }

        [Test]
        public void ListInstance_NoFileSystem_ShouldListRegistry()
        {
            registryStore.GetListFromRegistry()
                .Returns(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "configFilePath1"),
                    new ApplicationInstanceRecord("instance2", "configFilePath2")
                });
            fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

            var instances = instanceStore.ListInstances();
            instances.Should()
                .BeEquivalentTo(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "configFilePath1"),
                    new ApplicationInstanceRecord("instance2", "configFilePath2")
                });
        }

        [Test]
        public void ListInstance_ShouldPreferFileSystemEntries()
        {
            registryStore.GetListFromRegistry()
                .Returns(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "registryFilePath1"),
                    new ApplicationInstanceRecord("instance2", "registryFilePath2")
                });
            fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            fileSystem.EnumerateFiles(Arg.Any<string>()).Returns(new List<string> { "file1", "file2" });
            fileSystem.FileExists(Arg.Any<string>()).Returns(true);
            fileSystem.ReadFile(Arg.Is("file1")).Returns("{\"Name\": \"instance2\",\"ConfigurationFilePath\": \"fileConfigFilePath2\"}");
            fileSystem.ReadFile(Arg.Is("file2")).Returns("{\"Name\": \"instance3\",\"ConfigurationFilePath\": \"fileConfigFilePath3\"}");

            var instances = instanceStore.ListInstances();
            instances.Should()
                .BeEquivalentTo(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "registryFilePath1"),
                    new ApplicationInstanceRecord("instance2", "fileConfigFilePath2"),
                    new ApplicationInstanceRecord("instance3", "fileConfigFilePath3")
                });
        }

        [Test]
        public void GetInstance_ShouldPreferFileSystemEntries()
        {
            var configFilename = Path.Combine(instanceStore.InstancesFolder(), "instance-1.config");

            fileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            fileSystem.FileExists(Arg.Any<string>()).Returns(true);
            fileSystem.ReadFile(Arg.Is(configFilename)).Returns("{\"Name\": \"instance 1\",\"ConfigurationFilePath\": \"fileConfigFilePath2\"}");

            var instance = instanceStore.GetInstance("instance 1");
            instance.InstanceName.Should().Be("instance 1");
            instance.ConfigurationFilePath.Should().Be("fileConfigFilePath2");
        }

        [Test]
        public void GetInstance_ShouldReturnNullIfNoneFound()
        {
            registryStore.GetListFromRegistry()
                .Returns(new List<ApplicationInstanceRecord>
                {
                    new ApplicationInstanceRecord("instance1", "ServerPath1"),
                    new ApplicationInstanceRecord("instance2", "ServerPath2")
                });

            var instance = instanceStore.GetInstance("I AM FAKE");
            Assert.IsNull(instance);
        }

        [Test]
        public void MigrateInstance()
        {
            var sourceInstance = new ApplicationInstanceRecord("instance1", "configFilePath");
            registryStore.GetInstanceFromRegistry(Arg.Is("instance1")).Returns(sourceInstance);

            instanceStore.MigrateInstance(sourceInstance);
            fileSystem.Received().CreateDirectory(Arg.Any<string>());
            fileSystem.Received().OverwriteFile(Arg.Is<string>(x => x.Contains(sourceInstance.InstanceName)), Arg.Is<string>(x => x.Contains(sourceInstance.ConfigurationFilePath)));
        }
    }
}