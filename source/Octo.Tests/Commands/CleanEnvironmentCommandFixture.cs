﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Octopus.Cli.Commands;
using Octopus.Cli.Infrastructure;
using Octopus.Client.Model;
using FluentAssertions;
#pragma warning disable 618

namespace Octopus.Cli.Tests.Commands
{
    [TestFixture]
    public class CleanEnvironmentCommandFixture : ApiCommandFixtureBase
    {
        [SetUp]
        public void SetUp()
        {
            listMachinesCommand = new CleanEnvironmentCommand(RepositoryFactory, Log, FileSystem, ClientFactory);
        }

        CleanEnvironmentCommand listMachinesCommand;

        [Test]
        public void ShouldCleanEnvironmentWithEnvironmentAndStatusArgs()
        {
            CommandLineArgs.Add("-environment=Development");
            CommandLineArgs.Add("-status=Offline");

            Repository.Environments.FindByName("Development").Returns(
                new EnvironmentResource {Name = "Development", Id = "Environments-001"}
            );

            var machineList = new List<MachineResource>
            {
                new MachineResource
                {
                    Name = "PC01466",
                    Id = "Machines-002",
                    Status = MachineModelStatus.Offline,
                    EnvironmentIds = new ReferenceCollection("Environments-001")
                },
                new MachineResource
                {
                    Name = "PC01996",
                    Id = "Machines-003",
                    Status = MachineModelStatus.Offline,
                    EnvironmentIds = new ReferenceCollection("Environments-001")
                }
            };

            Repository.Machines.FindMany(Arg.Any<Func<MachineResource, bool>>()).Returns(machineList);

            listMachinesCommand.Execute(CommandLineArgs.ToArray()).GetAwaiter().GetResult();

            LogLines.Should().Contain(string.Format("[Information] Found {0} machines in {1} with the status {2}", machineList.Count, "Development", MachineModelStatus.Offline.ToString()));

            LogLines.Should().Contain(string.Format("[Information] Deleting {0} {1} (ID: {2})", machineList[0].Name, machineList[0].Status, machineList[0].Id));
            Repository.Machines.Received().Delete(machineList[0]);

            LogLines.Should().Contain(string.Format("[Information] Deleting {0} {1} (ID: {2})", machineList[1].Name, machineList[1].Status, machineList[1].Id));
            Repository.Machines.Received().Delete(machineList[1]);
        }

        [Test]
        public void ShouldRemoveMachinesBelongingToMultipleEnvironmentsInsteadOfDeleting()
        {
            CommandLineArgs.Add("-environment=Development");
            CommandLineArgs.Add("-status=Offline");

            Repository.Environments.FindByName("Development").Returns(
                new EnvironmentResource {Name = "Development", Id = "Environments-001"}
            );

            var machineList = new List<MachineResource>
            {
                new MachineResource
                {
                    Name = "PC01466",
                    Id = "Machines-002",
                    Status = MachineModelStatus.Offline,
                    EnvironmentIds = new ReferenceCollection(new[] {"Environments-001", "Environments-002"})
                },
                new MachineResource
                {
                    Name = "PC01996",
                    Id = "Machines-003",
                    Status = MachineModelStatus.Offline,
                    EnvironmentIds = new ReferenceCollection("Environments-001")
                }
            };

            Repository.Machines.FindMany(Arg.Any<Func<MachineResource, bool>>()).Returns(machineList);

            listMachinesCommand.Execute(CommandLineArgs.ToArray()).GetAwaiter().GetResult();

            LogLines.Should().Contain(string.Format("[Information] Found {0} machines in {1} with the status {2}", machineList.Count, "Development", MachineModelStatus.Offline.ToString()));
            LogLines.Should().Contain("[Information] Note: Some of these machines belong to multiple environments. Instead of being deleted, these machines will be removed from the Development environment.");

            LogLines.Should().Contain($"[Information] Removing {machineList[0].Name} {machineList[0].Status} (ID: {machineList[0].Id}) from Development");
            Assert.That(machineList[0].EnvironmentIds.Count, Is.EqualTo(1), "The machine should have been removed from the Development environment.");
            Repository.Machines.Received().Modify(machineList[0]);

            LogLines.Should().Contain(string.Format("[Information] Deleting {0} {1} (ID: {2})", machineList[1].Name, machineList[1].Status, machineList[1].Id));
            Repository.Machines.Received().Delete(machineList[1]);
        }

        [Test]
        public void ShouldNotCleanEnvironmentWithMissingEnvironmentArgs()
        {
            Func<Task> exec = () => listMachinesCommand.Execute(CommandLineArgs.ToArray());
            exec.ShouldThrow<CommandException>()
                .WithMessage("Please specify an environment name using the parameter: --environment=XYZ");
        }

        [Test]
        public void ShouldNotCleanEnvironmentWithMissingStatusArgs()
        {
            CommandLineArgs.Add("-environment=Development");
            Func<Task> exec = () => listMachinesCommand.Execute(CommandLineArgs.ToArray());
            exec.ShouldThrow<CommandException>()
              .WithMessage("Please specify a status using the parameter: --status or --health-status");
        }

        [Test]
        public void ShouldNotCleanTheEnvironmentIfEnvironmentNotFound()
        {
            CommandLineArgs.Add("-environment=Development");
            CommandLineArgs.Add("-status=Offline");

            Func<Task> exec = () => listMachinesCommand.Execute(CommandLineArgs.ToArray());
            exec.ShouldThrow<CouldNotFindException>()
              .WithMessage("Could not find the specified environment; either it does not exist or you lack permissions to view it.");
        }
    }
}
